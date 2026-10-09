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
