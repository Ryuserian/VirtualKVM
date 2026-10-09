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

        private Win32 win32Ctrl = new Win32();
        private KeyCodeTranslator keyCodeTranslator = new KeyCodeTranslator();

        // 연결 상태 워치독: 주기적으로 SSH/데몬/TCP 생존을 확인하고 자동 복구한다.
        private System.Windows.Forms.Timer watchdogTimer = null;
        private int watchdog_reboot_tries = 0;

        private int flagKeySwitch = 0;
        private bool keyAllowed = true;   // true = 로컬( 호스트 PC )로 입력, false = 원격으로 포워딩
        private bool mouseAllowed = true;

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

            Application.ApplicationExit += Application_ApplicationExit;

            raspberryCtrl = new RaspberryPi_Lib.Raspberry("192.168.0.101", "pi", "raspberry");
            remoteCtrl = raspberryCtrl.GetRemoteController();
            localCtrl = raspberryCtrl.GetLocalController();

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
            mouseAllowed = true;
            flagKeySwitch = 0;
            watchdog_reboot_tries = 0;
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
            mouseAllowed = true;

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

            // WM_KEYUP / WM_SYSKEYUP 만으로 구분하면 Alt 조합키(WM_SYSKEY*)에서
            // 키가 눌린 상태로 남아 "키 눌림 고착(stuck key)"과 연결 불안을 유발한다.
            bool isKeyUp =
                wParam == (IntPtr)Win32.WM_KEYUP ||
                wParam == (IntPtr)Win32.WM_SYSKEYUP;

            if (!isKeyUp)
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
                if (flagKeySwitch == 7)
                {
                    keyAllowed = !keyAllowed;
                    mouseAllowed = !mouseAllowed;

                    AllowSwitchKey[0] = 1;
                    AllowSwitchKey[1] = 1;
                    AllowSwitchKey[2] = 1;

                    // 전환 조합키를 원격에 Up 으로 flush 해서 원격 쪽 modifier 고착을 방지한다.
                    Flush_Modifier_Up(VK_LMENU);
                    Flush_Modifier_Up(VK_LCONTROL);
                    Flush_Modifier_Up(VK_LSHIFT);
                }

                switch (lParam.vkCode)
                {
                    case VK_LMENU:
                        {
                            flagKeySwitch &= ~1;
                            AllowSwitchKey[0] = 0;
                        }
                        break;
                    case VK_LSHIFT:
                        {
                            flagKeySwitch &= ~2;
                            AllowSwitchKey[1] = 0;
                        }
                        break;
                    case VK_LCONTROL:
                        {
                            flagKeySwitch &= ~4;
                            AllowSwitchKey[2] = 0;
                        }
                        break;
                }
            }

            bool switching_release = AllowSwitchKey[0] != 0 || AllowSwitchKey[1] != 0 || AllowSwitchKey[2] != 0;

            if (!keyAllowed)
            {
                if (!keyCodeTranslator.translateKey(lParam.vkCode, ref isSysKey, ref keyCode))
                    return true; // 매핑 없는 키는 로컬 처리

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

        private void Flush_Modifier_Up(int vkCode)
        {
            bool isSysKey = false;
            byte keyCode = 0;

            if (keyCodeTranslator.translateKey((uint)vkCode, ref isSysKey, ref keyCode))
                localCtrl.Keyboard_Up((RaspberryPi_Lib.Control_Key)keyCode);
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
        private const int WM_MOUSEWHEEL = 0x020A;

        private bool Win32Ctrl_MouseHookEvent(int nCode, IntPtr wParam, Win32.MouseHookStruct lParam)
        {
            // 제어권 획득 직후: 원격 전송 없이 현재 커서 위치를 기준점으로만 기록한다.
            if (mouseAllowed)
            {
                Win32.POINT pt;
                win32Ctrl.getCursorPos(out pt);

                prevX = pt.x;
                prevY = pt.y;

                mouseAllowed = !mouseAllowed;
            }

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
                        int dx = lParam.pt.x - prevX;
                        int dy = lParam.pt.y - prevY;
                        if (dx != 0 || dy != 0)
                        {
                            localCtrl.Mouse_Move(dx, dy);
                            prevX = lParam.pt.x;
                            prevY = lParam.pt.y;
                        }
                        break;
                }
            }

            return keyAllowed;
        }
    }
}
