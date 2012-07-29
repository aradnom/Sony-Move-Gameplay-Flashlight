using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using PSMoveSharp;
using WindowsInput;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;

// Class for handling all DLL invocations on behalf of the main loop
namespace MouseTest
{
    #region DirectX keys

    public struct DirectXKeyCodes
    {
        public const ushort DIK_W = 0x11;
        public const ushort DIK_A = 0x1E;
        public const ushort DIK_S = 0x1F;
        public const ushort DIK_D = 0x20;
        public const ushort DIK_R = 0x13;
    }

    #endregion

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT
    {
        public int uMsg;
        public short wParamL;
        public short wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct MouseKeybdHardwareInputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public MouseKeybdHardwareInputUnion mkhi;
    }
    
    class InputFunctions
    {
        // Win API control values used with SendInput
        const uint KEYEVENTF_DOWN = 0; //key DOWN
        const int INPUT_MOUSE = 0;
        const int INPUT_KEYBOARD = 1;
        const int INPUT_HARDWARE = 2;
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;
        const uint KEYEVENTF_SCANCODE = 0x0008;
        const uint XBUTTON1 = 0x0001;
        const uint XBUTTON2 = 0x0002;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_XDOWN = 0x0080;
        const uint MOUSEEVENTF_XUP = 0x0100;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern UInt32 SendInput(UInt32 nInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] INPUT[] pInputs, Int32 cbSize);

        // Mouse events with mouse_event, probably won't work right in game

        public static void DoLeftMouseClick(int x, int y)
        {
            mouse_event((uint)0x02, (uint)x, (uint)y, (uint)0, (UIntPtr)0);
        }

        public static void DoRightMouseClick(int x, int y)
        {
            mouse_event((uint)0x08 | (uint)0x10, (uint)x, (uint)y, (uint)0, (UIntPtr)0);
        }

        public static void DoUnclick(int type)
        {
            mouse_event((uint)type, (uint)0, (uint)0, (uint)0, (UIntPtr)0);
        }

        // SendInput functions, important for the... stuff...

        // Different from normal sendKey because it'll contain custom timing
        public static string sendKeyCustom()
        {
            INPUT[] InputData = new INPUT[2];
            //Key ScanCode = Microsoft.DirectX.DirectInput.Key.W;

            InputData[0].type = 1; // INPUT_KEYBOARD
            InputData[0].mkhi.ki.wScan = (ushort)0x11;
            InputData[0].mkhi.ki.dwFlags = (uint)KEYEVENTF_SCANCODE;
            InputData[0].mkhi.ki.time = 0;
            InputData[0].mkhi.ki.dwExtraInfo = IntPtr.Zero;

            //InputData[1].type = 1; // INPUT_KEYBOARD
            //InputData[1].ki.wScan = (ushort)ScanCode;
            //InputData[1].ki.dwFlags = (uint)(KEYEVENTF_KEYUP | KEYEVENTF_UNICODE);

            // Send keydown, return error or send success
            if (SendInput(1, InputData, Marshal.SizeOf(InputData[1])) == 0)
            {
                return (Marshal.GetLastWin32Error().ToString());
            }
            else
            {
                return "sent";
            }
        }

        private static MOUSEINPUT createMouseInput(int x, int y, uint data, uint t, uint flag)
        {
            MOUSEINPUT mi = new MOUSEINPUT();
            mi.dx = x;
            mi.dy = y;
            mi.mouseData = data;
            mi.time = t;
            //mi.dwFlags = MOUSEEVENTF_ABSOLUTE| MOUSEEVENTF_MOVE;
            mi.dwFlags = flag;
            return mi;
        }

        private static KEYBDINPUT createKeybdInput(short wVK, uint flag)
        {
            KEYBDINPUT i = new KEYBDINPUT();
            i.wVk = (ushort)wVK;
            i.wScan = 0;
            i.time = 0;
            i.dwExtraInfo = IntPtr.Zero;
            i.dwFlags = flag;
            return i;
        }

        private static short getCVal(char c)
        {
            if (c >= 'a' && c <= 'z') return (short)(c - 'a' + 0x41);
            else if (c >= '0' && c <= '9') return (short)(c - '0' + 0x30);
            else if (c == '-') return 0x6D; // Note it's NOT 0x2D as in ASCII code!
            else return 0; // default
        }

        public static string sim_mov(int x, int y)
        {
            INPUT[] inp = new INPUT[1];
            //inp[0].type = INPUT_MOUSE;
            //inp[0].mkhi.mi = createMouseInput(0, 0, 0, 0, MOUSEEVENTF_VIRTUALDESK | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE);

            inp[0].type = INPUT_MOUSE;
            inp[0].mkhi.mi = createMouseInput(x, y, 0, 0, MOUSEEVENTF_MOVE);

           // SendInput((uint)inp.Length, inp, Marshal.SizeOf(inp[0].GetType()));

            if (SendInput(1, inp, Marshal.SizeOf(inp[0])) == 0)
                return "not sent";
            else
                return "sent";
        }

        public static void sim_click( string type = "left" )
        {
            
            INPUT[] inp = new INPUT[2];
            uint flagUp, flagDown;

            inp[0].type = INPUT_MOUSE;
            inp[1].type = INPUT_MOUSE;

            if (type == "left")
            {
                flagUp = MOUSEEVENTF_LEFTUP;
                flagDown = MOUSEEVENTF_LEFTDOWN;
            }
            else
            {
                flagUp = MOUSEEVENTF_RIGHTUP;
                flagDown = MOUSEEVENTF_RIGHTDOWN;
            }

            inp[0].mkhi.mi = createMouseInput(0, 0, 0, 0, flagDown);
            inp[1].mkhi.mi = createMouseInput(0, 0, 0, 0, flagUp);

            SendInput((uint)inp.Length, inp, Marshal.SizeOf(inp[0].GetType()));
        }

        public static void sim_key( short keycode )
        {
            INPUT[] inp;
            
            inp = new INPUT[2];

            inp[0].type = INPUT_KEYBOARD;
            inp[0].mkhi.ki = createKeybdInput(keycode, 0);

            inp[1].type = INPUT_KEYBOARD;
            inp[1].mkhi.ki = createKeybdInput(keycode, KEYEVENTF_KEYUP);

            SendInput((uint)inp.Length, inp, Marshal.SizeOf(inp[0].GetType()));
        }

        public static void sim_type(string txt)
        {
            int i, len;
            char[] c_array;
            short c;
            INPUT[] inp;

            if (txt == null || txt.Length == 0) return;

            c_array = txt.ToCharArray();
            len = c_array.Length;
            inp = new INPUT[2];

            for (i = 0; i < len; i++)
            {
                c = getCVal(txt[i]);

                inp[0].type = INPUT_KEYBOARD;
                inp[0].mkhi.ki = createKeybdInput(c, 0);
                inp[1].type = INPUT_KEYBOARD;
                inp[1].mkhi.ki = createKeybdInput(c, KEYEVENTF_KEYUP);

                SendInput((uint)inp.Length, inp, Marshal.SizeOf(inp[0].GetType()));
            }
        }

        public static string keyDown( ushort keycode )
        {
            INPUT[] InputData = new INPUT[1];

            InputData[0].type = 1; // INPUT_KEYBOARD
            InputData[0].mkhi.ki.wScan = (ushort)keycode;
            InputData[0].mkhi.ki.dwFlags = (uint)KEYEVENTF_SCANCODE;
            InputData[0].mkhi.ki.time = 0;
            InputData[0].mkhi.ki.dwExtraInfo = IntPtr.Zero;

            // Send keydown, return error or send success
            if (SendInput(1, InputData, Marshal.SizeOf(InputData[0])) == 0)
                return (Marshal.GetLastWin32Error().ToString());
            else
                return true.ToString();
        }

        public static string keyUp(ushort keycode)
        {
            INPUT[] inp;

            inp = new INPUT[1];

            inp[0].type = (UInt32)InputType.KEYBOARD;
            //InputData[0].Vk = (ushort)DirectInputKeyScanCode;  //Virtual key is ignored when sending scan code
            inp[0].mkhi.ki.wScan = (ushort)keycode;
            inp[0].mkhi.ki.dwFlags = (uint)KeyboardFlag.KEYUP | (uint)KeyboardFlag.SCANCODE;
            inp[0].mkhi.ki.time = 0;
            inp[0].mkhi.ki.dwExtraInfo = IntPtr.Zero;

            if (SendInput(1, inp, Marshal.SizeOf(inp[0])) == 0)
                return (Marshal.GetLastWin32Error().ToString());
            else
                return true.ToString();
        }
    }
}
