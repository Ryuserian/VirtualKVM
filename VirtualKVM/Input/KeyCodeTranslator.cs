using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualKVM
{
    internal class KeyCodeTranslator
    {
        private struct eventStruct
        {
            public bool isSysKey;
            public byte keyCode;
            public String msg;
        }

        private eventStruct[] codeTable =
        {
            new eventStruct { isSysKey = false, keyCode = 0, msg = "0" }, // 0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "VK_LBUTTON" }, // VK_LBUTTON
            new eventStruct { isSysKey = false, keyCode = 0, msg = "VK_RBUTTON" }, // VK_RBUTTON
            new eventStruct { isSysKey = false, keyCode = 0, msg = "VK_CANCEL" }, // VK_CANCEL
            new eventStruct { isSysKey = false, keyCode = 0, msg = "VK_MBUTTON" }, // VK_MBUTTON
            new eventStruct { isSysKey = false, keyCode = 0, msg = "VK_XBUTTON1" }, // VK_XBUTTON1
            new eventStruct { isSysKey = false, keyCode = 0, msg = "VK_XBUTTON2" }, // VK_XBUTTON2
            new eventStruct { isSysKey = false, keyCode = 0, msg = "7" }, // 0x7
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Backspace, msg = "VK_BACK" }, // VK_BACK
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Tab, msg = "VK_TAB" }, // VK_TAB
            new eventStruct { isSysKey = false, keyCode = 0, msg = "a" }, // 0xa
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b" }, // 0xb
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c" }, // 0xc
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Enter, msg = "VK_ENTER" }, // VK_ENTER
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e" }, // 0xe
            new eventStruct { isSysKey = false, keyCode = 0, msg = "f" }, // 0xf
            // 0x10
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Left_Shift, msg = "VK_SHIFT" }, // VK_SHIFT
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Left_Ctrl, msg = "VK_CONTROL" }, // VK_CONTROL
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Left_Alt, msg = "VK_ALT" }, // VK_ALT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Pause, msg = "VK_PAUSE" }, // VK_PAUSE
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Capslock, msg = "VK_CAPSLOCK" }, // VK_CAPSLOCK
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Lang_Hangeul, msg = "VK_HANGEUL" }, // 0x15
            new eventStruct { isSysKey = false, keyCode = 0, msg = "16" }, // 0x16
            new eventStruct { isSysKey = false, keyCode = 0, msg = "17" }, // 0x17
            new eventStruct { isSysKey = false, keyCode = 0, msg = "18" }, // 0x18
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Lang_Hanja, msg = "VK_HANJA" }, // 0x19
            new eventStruct { isSysKey = false, keyCode = 0, msg = "1a" }, // 0x1a
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.ESC, msg = "VK_ESC" }, // VK_ESC
            new eventStruct { isSysKey = false, keyCode = 0, msg = "1c" }, // 0x1c
            new eventStruct { isSysKey = false, keyCode = 0, msg = "1d" }, // 0x1d
            new eventStruct { isSysKey = false, keyCode = 0, msg = "1e" }, // 0x1e
            new eventStruct { isSysKey = false, keyCode = 0, msg = "1f" }, // 0x1f
            // 0x20
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Space, msg = "VK_SPACE" }, // VK_SPACE
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.PageUp, msg = "VK_PAGEUP" }, // VK_PAGEUP
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.PageDown, msg = "VK_PAGEDOWN" }, // VK_PAGEDOWN
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.End, msg = "VK_END" }, // VK_END
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Home, msg = "VK_HOME" }, // VK_HOME
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Left, msg = "VK_LEFT" }, // VK_LEFT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Up, msg = "VK_UP" }, // VK_UP
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Right, msg = "VK_RIGHT" }, // VK_RIGHT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Down, msg = "VK_DOWN" }, // VK_DOWN
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Select, msg = "VK_SELECT" }, // VK_SELECT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Print, msg = "VK_PRINT" }, // VK_PRINT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Execute, msg = "VK_EXECUTE" }, // VK_EXECUTE
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Print, msg = "VK_SNAPSHOT" }, // VK_SNAPSHOT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Insert, msg = "VK_INSERT" }, // VK_INSERT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Delete, msg = "VK_DELETE" }, // VK_DELETE
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Help, msg = "VK_HELP" }, // VK_HELP
            // 0x30
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D0, msg = "D0" }, // 0
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D1, msg = "D1" }, // 1
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D2, msg = "D2" }, // 2
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D3, msg = "D3" }, // 3
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D4, msg = "D4" }, // 4
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D5, msg = "D5" }, // 5
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D6, msg = "D6" }, // 6
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D7, msg = "D7" }, // 7
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D8, msg = "D8" }, // 8
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D9, msg = "D9" }, // 9
            new eventStruct { isSysKey = false, keyCode = 0, msg = "3a" }, // 0x3a
            new eventStruct { isSysKey = false, keyCode = 0, msg = "3b" }, // 0x3b
            new eventStruct { isSysKey = false, keyCode = 0, msg = "3c" }, // 0x3c
            new eventStruct { isSysKey = false, keyCode = 0, msg = "3d" }, // 0x3d
            new eventStruct { isSysKey = false, keyCode = 0, msg = "3e" }, // 0x3e
            new eventStruct { isSysKey = false, keyCode = 0, msg = "3f" }, // 0x3f
            // 0x40
            new eventStruct { isSysKey = false, keyCode = 0 }, // 0x40
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.A, msg = "A" }, // a
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.B, msg = "B" }, // b
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.C, msg = "C" }, // c
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.D, msg = "D" }, // d
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.E, msg = "E" }, // e
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F, msg = "F" }, // f
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.G, msg = "G" }, // g
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.H, msg = "H" }, // h
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.I, msg = "I" }, // i
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.J, msg = "J" }, // j
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.K, msg = "K" }, // k
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.L, msg = "L" }, // l
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.M, msg = "M" }, // m
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.N, msg = "N" }, // n
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.O, msg = "O" }, // o
            // 0x50
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.P, msg = "P" }, // p
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Q, msg = "Q" }, // q
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.R, msg = "R" }, // r
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.S, msg = "S" }, // s
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.T, msg = "T" }, // t
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.U, msg = "U" }, // u
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.V, msg = "V" }, // v
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.W, msg = "W" }, // w
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.X, msg = "X" }, // x
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Y, msg = "Y" }, // y
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Z, msg = "Z" }, // z
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Left_Meta, msg = "VK_LWIN" }, // VK_LWIN
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Right_Meta, msg = "VK_RWIN" }, // VK_RWIN
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Application, msg = "VK_APPS" }, // VK_APPS
            new eventStruct { isSysKey = false, keyCode = 0, msg = "5e" }, // 0x5e
            new eventStruct { isSysKey = false, keyCode = 0, msg = "VK_SLEEP" }, // VK_SLEEP
            // 0x60
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_0, msg = "VK_NUMPAD0" }, // VK_NUMPAD0
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_1, msg = "VK_NUMPAD1" }, // VK_NUMPAD1
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_2, msg = "VK_NUMPAD2" }, // VK_NUMPAD2
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_3, msg = "VK_NUMPAD3" }, // VK_NUMPAD3
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_4, msg = "VK_NUMPAD4" }, // VK_NUMPAD4
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_5, msg = "VK_NUMPAD5" }, // VK_NUMPAD5
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_6, msg = "VK_NUMPAD6" }, // VK_NUMPAD6
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_7, msg = "VK_NUMPAD7" }, // VK_NUMPAD7
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_8, msg = "VK_NUMPAD8" }, // VK_NUMPAD8
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_9, msg = "VK_NUMPAD9" }, // VK_NUMPAD9
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_Multiply, msg = "VK_MULTIPLY" }, // VK_MULTIPLY
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_Plus, msg = "VK_ADD" }, // VK_ADD
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_Dot, msg = "VK_SEPERATOR" }, // VK_SEPERATOR
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_Minus, msg = "VK_SUBTRACT" }, // VK_SUBTRACT
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_Dot, msg = "VK_DECIMAL" }, // VK_DECIMAL
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.KeyPad_Divide, msg = "VK_DIVIDE" }, // VK_DIVIDE
            // 0x70
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F1, msg = "VK_F1" }, // VK_F1
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F2, msg = "VK_F2" }, // VK_F2
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F3, msg = "VK_F3" }, // VK_F3
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F4, msg = "VK_F4" }, // VK_F4
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F5, msg = "VK_F5" }, // VK_F5
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F6, msg = "VK_F6" }, // VK_F6
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F7, msg = "VK_F7" }, // VK_F7
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F8, msg = "VK_F8" }, // VK_F8
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F9, msg = "VK_F9" }, // VK_F9
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F10, msg = "VK_F10" }, // VK_F10
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F11, msg = "VK_F11" }, // VK_F11
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F12, msg = "VK_F12" }, // VK_F12
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F13, msg = "VK_F13" }, // VK_F13
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F14, msg = "VK_F14" }, // VK_F14
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F15, msg = "VK_F15" }, // VK_F15
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F16, msg = "VK_F16" }, // VK_F16
            // 0x80
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F17, msg = "VK_F17" }, // VK_F17
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F18, msg = "VK_F18" }, // VK_F18
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F19, msg = "VK_F19" }, // VK_F19
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F20, msg = "VK_F20" }, // VK_F20
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F21, msg = "VK_F21" }, // VK_F21
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F22, msg = "VK_F22" }, // VK_F22
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F23, msg = "VK_F23" }, // VK_F23
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.F24, msg = "VK_F24" }, // VK_F24
            new eventStruct { isSysKey = false, keyCode = 0, msg = "88" }, // 0x88
            new eventStruct { isSysKey = false, keyCode = 0, msg = "89" }, // 0x89
            new eventStruct { isSysKey = false, keyCode = 0, msg = "8a" }, // 0x8a
            new eventStruct { isSysKey = false, keyCode = 0, msg = "8b" }, // 0x8b
            new eventStruct { isSysKey = false, keyCode = 0, msg = "8c" }, // 0x8c
            new eventStruct { isSysKey = false, keyCode = 0, msg = "8d" }, // 0x8d
            new eventStruct { isSysKey = false, keyCode = 0, msg = "8e" }, // 0x8e
            new eventStruct { isSysKey = false, keyCode = 0, msg = "8f" }, // 0x8f
            // 0x90
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.NumLock, msg = "VK_NUMLOCK" }, // VK_NUMLOCK
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.ScrollLock, msg = "VK_SCROLL" }, // VK_SCROLL
            new eventStruct { isSysKey = false, keyCode = 0, msg = "92" }, // 0x92
            new eventStruct { isSysKey = false, keyCode = 0, msg = "93" }, // 0x93
            new eventStruct { isSysKey = false, keyCode = 0, msg = "94" }, // 0x94
            new eventStruct { isSysKey = false, keyCode = 0, msg = "95" }, // 0x95
            new eventStruct { isSysKey = false, keyCode = 0, msg = "96" }, // 0x96
            new eventStruct { isSysKey = false, keyCode = 0, msg = "97" }, // 0x97
            new eventStruct { isSysKey = false, keyCode = 0, msg = "98" }, // 0x98
            new eventStruct { isSysKey = false, keyCode = 0, msg = "99" }, // 0x99
            new eventStruct { isSysKey = false, keyCode = 0, msg = "9a" }, // 0x9a
            new eventStruct { isSysKey = false, keyCode = 0, msg = "9b" }, // 0x9b
            new eventStruct { isSysKey = false, keyCode = 0, msg = "9c" }, // 0x9c
            new eventStruct { isSysKey = false, keyCode = 0, msg = "9d" }, // 0x9d
            new eventStruct { isSysKey = false, keyCode = 0, msg = "9e" }, // 0x9e
            new eventStruct { isSysKey = false, keyCode = 0, msg = "9f" }, // 0x9f
            // 0xa0
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Left_Shift, msg = "VK_LSHIFT" }, // VK_LSHIFT
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Right_Shift, msg = "VK_RSHIFT" }, // VK_RSHIFT
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Left_Ctrl, msg = "VK_LCONTROL" }, // VK_LCONTROL
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Right_Ctrl, msg = "VK_RCONTROL" }, // VK_RCONTROL
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Left_Meta, msg = "VK_LMENU" }, // VK_LMENU
            new eventStruct { isSysKey = true, keyCode = (byte)RaspberryPi_Lib.Control_Key.Right_Meta, msg = "VK_RMENU" }, // VK_RMENU
            new eventStruct { isSysKey = false, keyCode = 0, msg = "a6" }, // 0xa6
            new eventStruct { isSysKey = false, keyCode = 0, msg = "a7" }, // 0xa7
            new eventStruct { isSysKey = false, keyCode = 0, msg = "a8" }, // 0xa8
            new eventStruct { isSysKey = false, keyCode = 0, msg = "a9" }, // 0xa9
            new eventStruct { isSysKey = false, keyCode = 0, msg = "aa" }, // 0xaa
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ab" }, // 0xab
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ac" }, // 0xac
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ad" }, // 0xad
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ae" }, // 0xae
            new eventStruct { isSysKey = false, keyCode = 0, msg = "af" }, // 0xaf
            // 0xb0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b0" }, // 0xb0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b1" }, // 0xb1
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b2" }, // 0xb2
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b3" }, // 0xb3
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b4" }, // 0xb4
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b5" }, // 0xb5
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b6" }, // 0xb6
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b7" }, // 0xb7
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b8" }, // 0xb8
            new eventStruct { isSysKey = false, keyCode = 0, msg = "b9" }, // 0xb9
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Semicolon, msg = "VK_;:" }, // 0xba
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Equal, msg = "VK_=+" }, // 0xbb
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Comma, msg = "VK_,<" }, // 0xbc
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Minus, msg = "VK_-_" }, // 0xbd
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Dot, msg = "VK_.>" }, // 0xbe
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Slash, msg = "VK_/?" }, // 0xbf
            // 0xc0
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Tilde, msg = "VK_`~" }, // 0xc0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c1" }, // 0xc1
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c2" }, // 0xc2
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c3" }, // 0xc3
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c4" }, // 0xc4
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c5" }, // 0xc5
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c6" }, // 0xc6
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c7" }, // 0xc7
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c8" }, // 0xc8
            new eventStruct { isSysKey = false, keyCode = 0, msg = "c9" }, // 0xc9
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ca" }, // 0xca
            new eventStruct { isSysKey = false, keyCode = 0, msg = "cb" }, // 0xcb
            new eventStruct { isSysKey = false, keyCode = 0, msg = "cc" }, // 0xcc
            new eventStruct { isSysKey = false, keyCode = 0, msg = "cd" }, // 0xcd
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ce" }, // 0xce
            new eventStruct { isSysKey = false, keyCode = 0, msg = "cf" }, // 0xcf
            // 0xd0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d0" }, // 0xd0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d1" }, // 0xd1
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d2" }, // 0xd2
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d3" }, // 0xd3
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d4" }, // 0xd4
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d5" }, // 0xd5
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d6" }, // 0xd6
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d7" }, // 0xd7
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d8" }, // 0xd8
            new eventStruct { isSysKey = false, keyCode = 0, msg = "d9" }, // 0xd9
            new eventStruct { isSysKey = false, keyCode = 0, msg = "da" }, // 0xda
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.LeftBracket, msg = "VK_[{" }, // 0xdb
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Backslash, msg = "VK_\\|" }, // 0xdc
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.RightBracket, msg = "VK_]}" }, // 0xdd
            new eventStruct { isSysKey = false, keyCode = (byte)RaspberryPi_Lib.Key.Quote, msg = "VK_'\"" }, // 0xde
            new eventStruct { isSysKey = false, keyCode = 0, msg = "df" }, // 0xdf
            // 0xe0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e0" }, // 0xe0
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e1" }, // 0xe1
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e2" }, // 0xe2
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e3" }, // 0xe3
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e4" }, // 0xe4
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e5" }, // 0xe5
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e6" }, // 0xe6
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e7" }, // 0xe7
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e8" }, // 0xe8
            new eventStruct { isSysKey = false, keyCode = 0, msg = "e9" }, // 0xe9
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ea" }, // 0xea
            new eventStruct { isSysKey = false, keyCode = 0, msg = "eb" }, // 0xeb
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ec" }, // 0xec
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ed" }, // 0xed
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ee" }, // 0xee
            new eventStruct { isSysKey = false, keyCode = 0, msg = "ef" }, // 0xef
        };

        public KeyCodeTranslator()
        {
        }

        public bool translateKey(uint virtualKey, ref bool isSysKey, ref byte keyCode)
        {
            if (0 <= virtualKey && virtualKey < codeTable.Length)
            {
                isSysKey = codeTable[virtualKey].isSysKey;
                keyCode = codeTable[virtualKey].keyCode;

                System.Diagnostics.Debug.WriteLine(string.Format("{0} {1} {2}", isSysKey, keyCode, codeTable[virtualKey].msg));
            }
            else
                return false;

            return true;
        }
    }
}
