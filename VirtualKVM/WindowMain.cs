using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private int flagKeySwitch = 0;
        private bool keyAllowed = true;
        private bool mouseAllowed = true; 
        private bool prevKeyAllowed = true;

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

            MNU_CONNECT.Click += MNU_CONNECT_Click;
            MNU_DISCONNECT.Click += MNU_DISCONNECT_Click;
            MNU_CONFIG.Click += MNU_CONFIG_Click;
            MNU_EXIT.Click += MNU_EXIT_Click;
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (remoteCtrl != null)
                remoteCtrl.Stop();

            if (localCtrl != null)
                localCtrl.Disconnect();

            if (raspberryCtrl != null)
                raspberryCtrl = null;
        }

        private void MNU_CONNECT_Click(object sender, EventArgs e)
        {
            win32Ctrl.KeyHookEvent += Win32Ctrl_KeyHookEvent;
            win32Ctrl.MouseHookEvent += Win32Ctrl_MouseHookEvent;

            if (!win32Ctrl.InstallHook())
                return;

            if (!remoteCtrl.Start())
                return;

            if (!localCtrl.Connect())
                return;

            MNU_CONNECT.Enabled = false;
            MNU_DISCONNECT.Enabled = true;
        }

        private void MNU_DISCONNECT_Click(object sender, EventArgs e)
        {
            if (!localCtrl.Disconnect())
                return;

            if (!remoteCtrl.Stop())
                return;

            if (!win32Ctrl.UninsatllHook())
                return;

            win32Ctrl.KeyHookEvent -= Win32Ctrl_KeyHookEvent;
            win32Ctrl.MouseHookEvent -= Win32Ctrl_MouseHookEvent;

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

        private bool Win32Ctrl_KeyHookEvent(int nCode, IntPtr wParam, Win32.KeyHookStruct lParam)
        {
            bool isSysKey = false;
            byte keyCode = 0;
            bool isKeyUp = false;
            bool SwitchingRelease = false;

            if (wParam == (IntPtr)Win32.WM_KEYDOWN)
                isKeyUp = false;
            if (wParam == (IntPtr)Win32.WM_KEYUP)
                isKeyUp = true;

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

                    if (keyAllowed)
                    {
                        if (!keyCodeTranslator.translateKey(VK_LMENU, ref isSysKey, ref keyCode))
                            return true;

                        localCtrl.Keyboard_Up((RaspberryPi_Lib.Control_Key)keyCode);

                        if (!keyCodeTranslator.translateKey(VK_LCONTROL, ref isSysKey, ref keyCode))
                            return true;

                        localCtrl.Keyboard_Up((RaspberryPi_Lib.Control_Key)keyCode);

                        if (!keyCodeTranslator.translateKey(VK_LSHIFT, ref isSysKey, ref keyCode))
                            return true;

                        localCtrl.Keyboard_Up((RaspberryPi_Lib.Control_Key)keyCode);
                    }
                }

                switch (lParam.vkCode)
                {
                    case VK_LMENU:
                        {
                            flagKeySwitch &= ~1;
                            SwitchingRelease = (AllowSwitchKey[0] == 0);
                            AllowSwitchKey[0] = 0;
                        }
                        break;
                    case VK_LSHIFT:
                        {
                            flagKeySwitch &= ~2;
                            SwitchingRelease = (AllowSwitchKey[1] == 0);
                            AllowSwitchKey[1] = 0;
                        }
                        break;
                    case VK_LCONTROL:
                        {
                            flagKeySwitch &= ~4;
                            SwitchingRelease = (AllowSwitchKey[2] == 0);
                            AllowSwitchKey[2] = 0;
                        }
                        break;
                }
            }

            if (!keyAllowed)
            {
                if (!keyCodeTranslator.translateKey(lParam.vkCode, ref isSysKey, ref keyCode))
                    return true;

                if (isSysKey)
                {
                    if (isKeyUp)
                    {
                        localCtrl.Keyboard_Up((RaspberryPi_Lib.Control_Key)keyCode);
                    }
                    else
                    {
                        localCtrl.Keyboard_Down((RaspberryPi_Lib.Control_Key)keyCode);
                    }
                }
                else
                {
                    if (isKeyUp)
                    {
                        localCtrl.Keyboard_Up((RaspberryPi_Lib.Key)keyCode);
                    }
                    else
                    {
                        localCtrl.Keyboard_Down((RaspberryPi_Lib.Key)keyCode);
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"{lParam.vkCode} {isKeyUp} {isSysKey} {prevKeyAllowed} {keyAllowed}");

            return (keyAllowed || SwitchingRelease);
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
                        //System.Diagnostics.Debug.WriteLine($"1111 {lParam.pt.x - prevX} {lParam.pt.y - prevY} {lParam.pt.x} {lParam.pt.y}");
                        localCtrl.Mouse_Move(lParam.pt.x - prevX, lParam.pt.y - prevY);
                        break;
                }
            }

            return keyAllowed;
        }
    }
}
