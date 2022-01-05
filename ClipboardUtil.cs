using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BorderlessGraphicViewer
{
    /// <summary>
    /// based on https://stackoverflow.com/a/21311270
    /// </summary>
    public class ClipboardUtil
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        ///-----------------------------------------------------------------------------
        /// <summary>
        /// Gets the Process that's holding the clipboard
        /// </summary>
        /// <returns>A Process object holding the clipboard, or null</returns>
        ///-----------------------------------------------------------------------------
        public static Process ProcessHoldingClipboard()
        {
            Process theProc = null;

            var hwnd = GetOpenClipboardWindow();

            if (hwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out var processId);

                Process[] procs = Process.GetProcesses();
                foreach (Process proc in procs)
                {
                    IntPtr handle = proc.MainWindowHandle;

                    if (handle == hwnd)
                    {
                        theProc = proc;
                    }
                    else if (processId == proc.Id)
                    {
                        theProc = proc;
                    }
                }
            }

            return theProc;
        }
    }
}
