using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VirtualKVM
{
    internal class Win32 : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_SYSKEYDOWN = 0x104;
        public const int WM_SYSKEYUP = 0x105;

        IntPtr hInstance = IntPtr.Zero;
        private IntPtr key_hook = IntPtr.Zero;
        private IntPtr mouse_hook = IntPtr.Zero;

        // 네이티브 후킹에 사용하는 delegate 인스턴스를 필드에 보관한다.
        // (미보관 시 GC 가 콜백을 회수하여 프로세스 크래시로 이어질 수 있다)
        private LowLevelProc key_hook_proc = null;
        private LowLevelProc mouse_hook_proc = null;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate bool delegateKeyProc(int nCode, IntPtr wParam, KeyHookStruct lParam);
        public delegate bool delegateMouseProc(int nCode, IntPtr wParam, MouseHookStruct lParam);

        public event delegateKeyProc KeyHookEvent = null;
        public event delegateMouseProc MouseHookEvent = null;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseHookStruct
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyHookStruct
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        #region Constructor

        public Win32()
        {
            hInstance = LoadLibrary("User32");
        }

        #endregion

        #region Public Method

        public void Dispose()
        {
            UninsatllHook();

            if (hInstance != IntPtr.Zero)
                FreeLibrary(hInstance);
        }

        public bool InstallHook()
        {
            if (hInstance == IntPtr.Zero)
                hInstance = LoadLibrary("User32");

            // delegate 인스턴스를 필드에 고정 후 후킹 등록 (GC 콜백 크래시 방지)
            key_hook_proc = KeyHookProc;
            mouse_hook_proc = MouseHookProc;

            key_hook = SetWindowsHookEx(WH_KEYBOARD_LL, key_hook_proc, hInstance, 0);
            mouse_hook = SetWindowsHookEx(WH_MOUSE_LL, mouse_hook_proc, hInstance, 0);

            return key_hook != IntPtr.Zero && mouse_hook != IntPtr.Zero;
        }

        public bool UninsatllHook()
        {
            if (key_hook != IntPtr.Zero)
                UnhookWindowsHookEx(key_hook);

            key_hook = IntPtr.Zero;

            if (mouse_hook != IntPtr.Zero)
                UnhookWindowsHookEx(mouse_hook);

            mouse_hook= IntPtr.Zero;

            return true;
        }

        public void getCursorPos(out POINT pt)
        {
            GetCursorPos(out pt);
        }

        #endregion

        #region Raw Input (relative mouse motion)

        // WH_MOUSE_LL 훅이 입력을 차단(return 1)하면 로컬 커서가 정지하므로
        // lParam.pt 로 상대 델타를 계산하는 방식은 신뢰할 수 없다(기준점 유실/과전송).
        // RawInput 은 차단 여부와 무관하게 하드웨어의 상대 이동량(lLastX/lLastY)을 주므로
        // 마우스 포워딩의 입력 소스로 사용한다.
        public const int WM_INPUT = 0x00FF;
        public const uint RIDEV_INPUTSINK = 0x00000100;
        public const uint RIDEV_REMOVE = 0x00000001;
        public const uint RID_INPUT = 0x10000000;
        public const uint RIM_TYPEMOUSE = 0;

        public struct MouseInput
        {
            public int dx;
            public int dy;
            public ushort buttonFlags; // RI_MOUSE_* 전이(transition) 플래그
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        public bool RegisterMouse(IntPtr hwnd_target)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // Generic Desktop
            rid[0].usUsage = 0x02;     // Mouse
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = hwnd_target;

            return RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        public void UnregisterMouse()
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;
            rid[0].dwFlags = RIDEV_REMOVE;
            rid[0].hwndTarget = IntPtr.Zero;

            RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        public bool ReadMouse(IntPtr h_raw_input, out MouseInput mi)
        {
            mi = new MouseInput();

            uint header_size = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            uint size = 0;

            if (GetRawInputData(h_raw_input, RID_INPUT, IntPtr.Zero, ref size, header_size) == 0 || size == 0)
                return false;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetRawInputData(h_raw_input, RID_INPUT, buffer, ref size, header_size) != size)
                    return false;

                RAWINPUTHEADER header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
                if (header.dwType != RIM_TYPEMOUSE)
                    return false;

                // RAWMOUSE 레이아웃(pack 기준)
                //  +0  usFlags        | +4  usButtonFlags  | +6  usButtonData
                //  +8  ulRawButtons   | +12 lLastX         | +16 lLastY
                IntPtr mouse_ptr = IntPtr.Add(buffer, (int)header_size);

                ushort us_flags = (ushort)Marshal.ReadInt16(mouse_ptr, 0);
                mi.buttonFlags = (ushort)Marshal.ReadInt16(mouse_ptr, 4);
                mi.dx = Marshal.ReadInt32(mouse_ptr, 12);
                mi.dy = Marshal.ReadInt32(mouse_ptr, 16);

                // 절대 좌표 장치(터치패드 등)는 상대 리포트에 매핑할 수 없어 이동만 무시한다.
                if ((us_flags & 0x01) != 0)
                {
                    mi.dx = 0;
                    mi.dy = 0;
                }

                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        #endregion

        #region Private Method

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseHookEvent != null)
                    if (!MouseHookEvent(nCode, wParam, (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct))))
                        return (IntPtr)1;
            }
            return CallNextHookEx(mouse_hook, nCode, wParam, lParam);
        }

        private IntPtr KeyHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            // WM_SYSKEYDOWN/WM_SYSKEYUP(Alt 조합 등)도 처리해야 시스템 키 Up 유실(고착)을 막을 수 있다.
            if (code >= 0 &&
                (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP ||
                 wParam == (IntPtr)WM_SYSKEYDOWN || wParam == (IntPtr)WM_SYSKEYUP))
            {
                if (KeyHookEvent != null)
                    if (!KeyHookEvent(code, wParam, (KeyHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyHookStruct))))
                        return (IntPtr)1;
            }

            return CallNextHookEx(key_hook, code, wParam, lParam);
        }

        #endregion
    }
}
