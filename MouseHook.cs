using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BorderlessGraphicViewer
{
    /// <summary>
    /// Edited from https://stackoverflow.com/questions/22659925/how-to-capture-mouseup-event-outside-the-wpf-window
    /// </summary>
    public static class MouseHook
    {
        public struct MouseState
        {
            public bool IsLeftButtonReleased { get; set; }
            public bool IsRightButtonReleased { get; set; }
        }
        public delegate void MouseUpEventHandler(object sender, MouseState mouseState);

        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        private static int _mouseHookHandle;
        private static HookProc _mouseDelegate;

        private static event MouseUpEventHandler MouseUp;
        public static event MouseUpEventHandler OnMouseUp
        {
            add
            {
                Subscribe();
                MouseUp += value;
            }
            remove
            {
                MouseUp -= value;
                Unsubscribe();
            }
        }

        private static void Unsubscribe()
        {
            if (_mouseHookHandle != 0)
            {
                int result = UnhookWindowsHookEx(_mouseHookHandle);
                _mouseHookHandle = 0;
                _mouseDelegate = null;
                if (result == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }

        private static void Subscribe()
        {
            if (_mouseHookHandle == 0)
            {
                _mouseDelegate = MouseHookProc;
                _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL,
                    _mouseDelegate,
                    GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName),
                    0);
                if (_mouseHookHandle == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }

        private static int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var mouseHookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                if (wParam == WM_LBUTTONUP || wParam == WM_RBUTTONUP)
                {
                    if (MouseUp != null)
                    {
                        var mouseState = new MouseState
                        {
                            IsLeftButtonReleased = wParam == WM_LBUTTONUP,
                            IsRightButtonReleased = wParam == WM_RBUTTONUP
                        };
                        MouseUp.Invoke(null, mouseState);
                    }
                }
            }
            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
           CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
             CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);
    }

}
