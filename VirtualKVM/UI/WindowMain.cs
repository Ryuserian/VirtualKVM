using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace VirtualKVM
{
    public partial class WindowMain : Form
    {
        private RaspberryPi_Lib.Raspberry raspberryCtrl = null;
        private RaspberryPi_Lib.Raspberry_RemoteCtrl remoteCtrl = null;
        private RaspberryPi_Lib.Raspberry_LocalCtrl localCtrl = null;

        // 연결 설정(IP/포트/계정): %APPDATA%\VirtualKVM\config.json 에 저장된다.
        private AppConfig config = null;

        private Win32 win32Ctrl = new Win32();
        private KeyCodeTranslator keyCodeTranslator = new KeyCodeTranslator();

        // 연결 상태 워치독: 주기적으로 SSH/데몬/TCP 생존을 확인하고 자동 복구한다.
        private System.Windows.Forms.Timer watchdogTimer = null;
        private int watchdog_reboot_tries = 0;

        private int flagKeySwitch = 0;
        private bool keyAllowed = true;   // true = 로컬( 호스트 PC )로 입력, false = 원격으로 포워딩

        private const int VK_LSHIFT = 0xa0;
        private const int VK_LCONTROL = 0xa2;
        private const int VK_LMENU = 0xa4;

        private int[] AllowSwitchKey = new int[] { 0, 0, 0 };

        public WindowMain()
        {
            InitializeComponent();
        }

        private void WindowMain_Load(object sender, EventArgs e)
        {
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            notifyIcon.Icon = VirtualKVM.Properties.Resources.ico_normal;

            // Windows 11 은 신규 트레이 아이콘을 ^ 접힘 영역으로 숨긴다.
            // 풍선 알림으로 실행 사실과 조작 방법을 명시적으로 안내한다.
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(4000, "VirtualKVM", "시스템 트레이에서 실행 중입니다.\n트레이 아이콘(^ 접힘 영역 포함)을 우클릭하여 연결/설정하세요.", ToolTipIcon.Info);

            Application.ApplicationExit += Application_ApplicationExit;

            config = AppConfig.Load();

            raspberryCtrl = new RaspberryPi_Lib.Raspberry(config.IpAddress, config.SshPort, config.UserId, config.PlainPassword);
            remoteCtrl = raspberryCtrl.GetRemoteController();
            localCtrl = raspberryCtrl.GetLocalController(config.HidPort);

            watchdogTimer = new System.Windows.Forms.Timer();
            watchdogTimer.Interval = 5000;
            watchdogTimer.Tick += WatchdogTimer_Tick;

            MNU_CONNECT.Click += MNU_CONNECT_Click;
            MNU_DISCONNECT.Click += MNU_DISCONNECT_Click;
            MNU_CONFIG.Click += MNU_CONFIG_Click;
            MNU_EXIT.Click += MNU_EXIT_Click;
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (watchdogTimer != null)
                watchdogTimer.Stop();

            if (remoteCtrl != null)
                remoteCtrl.Stop();

            if (localCtrl != null)
                localCtrl.Disconnect();

            raspberryCtrl = null;
        }

        private void MNU_CONNECT_Click(object sender, EventArgs e)
        {
            SetTrayIcon(VirtualKVM.Properties.Resources.ico_normal);

            if (!remoteCtrl.Start())
            {
                SetTrayIcon(VirtualKVM.Properties.Resources.ico_error);
                return;
            }

            if (!localCtrl.Connect())
            {
                remoteCtrl.Stop();
                SetTrayIcon(VirtualKVM.Properties.Resources.ico_error);
                return;
            }

            win32Ctrl.KeyHookEvent += Win32Ctrl_KeyHookEvent;
            win32Ctrl.MouseHookEvent += Win32Ctrl_MouseHookEvent;

            if (!win32Ctrl.InstallHook())
            {
                localCtrl.Disconnect();
                remoteCtrl.Stop();
                SetTrayIcon(VirtualKVM.Properties.Resources.ico_error);
                return;
            }

            keyAllowed = true;
            flagKeySwitch = 0;
            watchdog_reboot_tries = 0;

            // 마우스 기준점을 현재 커서 위치로 초기화 (전환 시 커서 점프 방지)
            Win32.POINT cursor_pt;
            win32Ctrl.getCursorPos(out cursor_pt);
            prevX = cursor_pt.x;
            prevY = cursor_pt.y;

            watchdogTimer.Start();

            SetTrayIcon(VirtualKVM.Properties.Resources.ico_linked);

            MNU_CONNECT.Enabled = false;
            MNU_DISCONNECT.Enabled = true;
        }

        private void MNU_DISCONNECT_Click(object sender, EventArgs e)
        {
            if (watchdogTimer != null)
                watchdogTimer.Stop();

            // 순서 중요: 입력 포워딩 중단 -> 훅 해제 -> 데몬/TCP 종료
            keyAllowed = true;

            win32Ctrl.UninsatllHook();
            win32Ctrl.KeyHookEvent -= Win32Ctrl_KeyHookEvent;
            win32Ctrl.MouseHookEvent -= Win32Ctrl_MouseHookEvent;

            localCtrl.Disconnect();
            remoteCtrl.Stop();

            SetTrayIcon(VirtualKVM.Properties.Resources.ico_normal);

            MNU_CONNECT.Enabled = true;
            MNU_DISCONNECT.Enabled = false;
        }

        private void MNU_CONFIG_Click(object sender, EventArgs e)
        {
            using (WindowConfig window = new WindowConfig(config))
            {
                if (window.ShowDialog() != DialogResult.OK)
                    return;

                // 연결 유지 중에 설정이 바뀌면 현재 연결을 걷어내고 새 설정으로 재구성한다.
                // (훅은 유지 — 전환키 토글 동작에는 영향 없음)
                bool was_connected = MNU_DISCONNECT.Enabled;

                if (was_connected)
                {
                    keyAllowed = true;

                    if (watchdogTimer != null)
                        watchdogTimer.Stop();

                    if (localCtrl != null)
                        localCtrl.Disconnect();
                    if (remoteCtrl != null)
                        remoteCtrl.Stop();
                }

                raspberryCtrl = new RaspberryPi_Lib.Raspberry(config.IpAddress, config.SshPort, config.UserId, config.PlainPassword);
                remoteCtrl = raspberryCtrl.GetRemoteController();
                localCtrl = raspberryCtrl.GetLocalController(config.HidPort);

                // 이전 연결이 살아있었다면 새 설정으로 즉시 재접속을 시도한다.
                if (was_connected)
                    Try();
            }
        }

        /// <summary>
        /// 새 설정으로 연결(SSH/데몬 -> HID TCP)을 시도하고 결과를 반영한다.
        /// 성공/실패 모두 트레이 아이콘 상태로 표시한다.
        /// </summary>
        private void Try()
        {
            if (!remoteCtrl.Start())
            {
                SetTrayIcon(VirtualKVM.Properties.Resources.ico_error);
                return;
            }

            if (!localCtrl.Connect())
            {
                remoteCtrl.Stop();
                SetTrayIcon(VirtualKVM.Properties.Resources.ico_error);
                return;
            }

            watchdog_reboot_tries = 0;
            watchdogTimer.Start();
            SetTrayIcon(VirtualKVM.Properties.Resources.ico_linked);
        }

        private void MNU_EXIT_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SetTrayIcon(System.Drawing.Icon icon)
        {
            notifyIcon.Icon = icon;
        }

        /// <summary>
        /// 5초 주기 연결 감시.
        ///  - TCP 끊김: 로컬 컨트롤러가 자동 재접속(하트비트 포함).
        ///  - SSH/데몬 죽음: 원격 컨트롤러 재시작. 연속 실패 시 Pi 재부팅.
        /// </summary>
        private void WatchdogTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                localCtrl.Watchdog_Pulse();

                if (!remoteCtrl.IsAlive())
                {
                    if (!remoteCtrl.Restart())
                    {
                        if (++watchdog_reboot_tries >= 3)
                        {
                            watchdog_reboot_tries = 0;
                            remoteCtrl.Reboot_Host();
                        }
                    }
                    else
                    {
                        watchdog_reboot_tries = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Watchdog error: " + ex.Message);
            }
        }

        private bool Win32Ctrl_KeyHookEvent(int nCode, IntPtr wParam, Win32.KeyHookStruct lParam)
        {
            bool isSysKey = false;
            byte keyCode = 0;
            // 지금 이 이벤트가 "전환 조합키의 release"인지 여부 (해당 이벤트만 로컬 통과)
            bool switching_release = false;

            // WM_KEYUP / WM_SYSKEYUP 만으로 구분하면 Alt 조합키(WM_SYSKEY*)에서
            // 키가 눌린 상태로 남아 "키 눌림 고착(stuck key)"과 연결 불안을 유발한다.
            bool isKeyUp =
                wParam == (IntPtr)Win32.WM_KEYUP ||
                wParam == (IntPtr)Win32.WM_SYSKEYUP;

            // KBDLLHOOKSTRUCT.flags bit4(0x10) = 키 반복 autorepeat.
            // 반복 DOWN 을 무시하지 않으면 전환 조합키를 길게 눌렀을 때
            // 토글 후 플래그가 즉시 다시 채워져 오토-토글이 일어난다.
            bool is_repeat = !isKeyUp && (lParam.flags & 0x10) != 0;

            if (!isKeyUp && !is_repeat)
            {
                switch (lParam.vkCode)
                {
                    case VK_LMENU:
                        flagKeySwitch |= 1;
                        break;
                    case VK_LSHIFT:
                        flagKeySwitch |= 2;
                        break;
                    case VK_LCONTROL:
                        flagKeySwitch |= 4;
                        break;
                }
            }
            else
            {
                bool isSwitchKey =
                    lParam.vkCode == VK_LMENU ||
                    lParam.vkCode == VK_LSHIFT ||
                    lParam.vkCode == VK_LCONTROL;

                // 조합키 세 개가 모두 눌린 상태에서 "전환 조합키 중 하나"가
                // 놓여질 때만 제어권을 토글한다.
                // (조합키와 무관한 키가 먼저 놓여도 토글되던 버그 제거)
                if (isSwitchKey && flagKeySwitch == 7)
                {
                    keyAllowed = !keyAllowed;
                    flagKeySwitch = 0; // 다음 토글은 조합키를 다시 모두 눌러야 발동

                    AllowSwitchKey[0] = 1;
                    AllowSwitchKey[1] = 1;
                    AllowSwitchKey[2] = 1;

                    // 원격에 눌려 있던 키/모디파이어/마우스 버튼을 모두 해제하여
                    // 원격 쪽 고착(stuck)을 방지한다.
                    localCtrl.Flush_Pressed_State();
                }

                // switching_release 는 "방금 놓인 전환 키의 release 이벤트"에만 true.
                // (전환 키가 눌려 있는 동안 다른 키까지 로컬로 새던 버그 제거)
                switch (lParam.vkCode)
                {
                    case VK_LMENU:
                        flagKeySwitch &= ~1;
                        switching_release = AllowSwitchKey[0] != 0;
                        AllowSwitchKey[0] = 0;
                        break;
                    case VK_LSHIFT:
                        flagKeySwitch &= ~2;
                        switching_release = AllowSwitchKey[1] != 0;
                        AllowSwitchKey[1] = 0;
                        break;
                    case VK_LCONTROL:
                        flagKeySwitch &= ~4;
                        switching_release = AllowSwitchKey[2] != 0;
                        AllowSwitchKey[2] = 0;
                        break;
                }
            }

            if (!keyAllowed)
            {
                if (!keyCodeTranslator.translateKey(lParam.vkCode, ref isSysKey, ref keyCode))
                {
                    // 매핑 없는 키: 원격에는 보내지 않는다. (전환 키 release 경로만 로컬 통과)
                    return switching_release;
                }

                if (isSysKey)
                {
                    if (isKeyUp)
                        localCtrl.Keyboard_Up((RaspberryPi_Lib.Control_Key)keyCode);
                    else
                        localCtrl.Keyboard_Down((RaspberryPi_Lib.Control_Key)keyCode);
                }
                else
                {
                    if (isKeyUp)
                        localCtrl.Keyboard_Up((RaspberryPi_Lib.Key)keyCode);
                    else
                        localCtrl.Keyboard_Down((RaspberryPi_Lib.Key)keyCode);
                }
            }

            Debug.WriteLine(string.Format("{0} {1} {2} {3}", lParam.vkCode, isKeyUp, isSysKey, keyAllowed));

            // 로컬 제어 중이거나, 전환 조합키의 release 이벤트면 로컬로 통과시킨다.
            return keyAllowed || switching_release;
        }

        private int prevX = 0;
        private int prevY = 0;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MOUSEMOVE = 0x0200;

        private bool Win32Ctrl_MouseHookEvent(int nCode, IntPtr wParam, Win32.MouseHookStruct lParam)
        {
            // 기준점(prevX/prevY)을 모든 이벤트에서 갱신한다.
            // 과거 구현은 mouseAllowed 플래그로 최초 1회만 기준점을 갱신해
            // 로컬 사용 중 커서가 옮겨도 prev 가 낡아, 원격 전환 첫 이동에서
            // 커서가 크게 튀는 점프 버그가 있었다.
            int dx = lParam.pt.x - prevX;
            int dy = lParam.pt.y - prevY;
            prevX = lParam.pt.x;
            prevY = lParam.pt.y;

            if (!keyAllowed)
            {
                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                        localCtrl.Mouse_Down(RaspberryPi_Lib.MouseButton.Left);
                        break;
                    case WM_LBUTTONUP:
                        localCtrl.Mouse_Up(RaspberryPi_Lib.MouseButton.Left);
                        break;
                    case WM_MBUTTONDOWN:
                        localCtrl.Mouse_Down(RaspberryPi_Lib.MouseButton.Middle);
                        break;
                    case WM_MBUTTONUP:
                        localCtrl.Mouse_Up(RaspberryPi_Lib.MouseButton.Middle);
                        break;
                    case WM_RBUTTONDOWN:
                        localCtrl.Mouse_Down(RaspberryPi_Lib.MouseButton.Right);
                        break;
                    case WM_RBUTTONUP:
                        localCtrl.Mouse_Up(RaspberryPi_Lib.MouseButton.Right);
                        break;
                    case WM_MOUSEMOVE:
                        if (dx != 0 || dy != 0)
                            localCtrl.Mouse_Move(dx, dy);
                        break;
                    // WM_MOUSEWHEEL / WM_MOUSEHWHEEL:
                    // USB HID mouse report_desc 에 휠 축이 없어 지원하지 않는다.
                    // (지원하려면 report_desc + 펌웨어 + 클라이언트 동시 수정 필요)
                }
            }

            return keyAllowed;
        }
    }
}
