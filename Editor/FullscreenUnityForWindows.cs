#if UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Neonage.SmartTools
{
    // Hides topmost Windows app bar (except MenuItems, as it's currently impossible to recreate them in-Editor due to lack of APIs)
    // Made with AI, tested only on Windows 10
    [InitializeOnLoad]
    static class FullscreenUnityForWindows
    {
        static FullscreenUnityForWindows()
        {
            EditorApplication.delayCall += OnEditorLoad;
        }
        static void OnEditorLoad()
        {
            if (EditorPrefs.GetBool(PREF_IS_BORDERLESS, false))
                EnforceBorderlessState();
        }

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        const uint GW_OWNER = 4;

        static IntPtr GetMainWindowHandle()
        {
            var mainHWnd = IntPtr.Zero;
            var threadId = GetCurrentThreadId();

            EnumThreadWindows(threadId, (hWnd, lParam) =>
            {
                var className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                string windowClassName = className.ToString();

                if (windowClassName == "UnityContainerWndClass" || windowClassName == "UnityWndClass")
                {
                    var owner = GetWindow(hWnd, GW_OWNER); // floating windows have owner assigned, on main window it's null
                    if (owner == IntPtr.Zero)
                    {
                        mainHWnd = hWnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return mainHWnd;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        const int GWL_STYLE = -16;
        const int WS_POPUP = unchecked((int)0x80000000);
        const int WS_CAPTION = 0x00C00000;
        const int WS_THICKFRAME = 0x00040000;
        const int WS_MINIMIZEBOX = 0x00020000;
        const int WS_MAXIMIZEBOX = 0x00010000;
        const int WS_SYSMENU = 0x00080000;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_FRAMECHANGED = 0x0020;
        const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        static readonly string PREF_PREFIX = $"FullscreenUnity_";
        static readonly string PREF_IS_BORDERLESS = $"{PREF_PREFIX}_IsEnabled";
        static readonly string PREF_STYLE = $"{PREF_PREFIX}_Style";
        static readonly string PREF_RECT_L = $"{PREF_PREFIX}_RectL";
        static readonly string PREF_RECT_T = $"{PREF_PREFIX}_RectT";
        static readonly string PREF_RECT_R = $"{PREF_PREFIX}_RectR";
        static readonly string PREF_RECT_B = $"{PREF_PREFIX}_RectB";
    

        [MenuItem("Tools/Toggle Borderless Editor _F11")]
        public static void ToggleBorderless()
        {
            var hWnd = GetMainWindowHandle();
            if (hWnd == IntPtr.Zero) 
                return;

            var wasBorderless = EditorPrefs.GetBool(PREF_IS_BORDERLESS, false);
            EditorPrefs.SetBool(PREF_IS_BORDERLESS, !wasBorderless);

            if (!wasBorderless)
            {
                int style = GetWindowLong(hWnd, GWL_STYLE);
                GetWindowRect(hWnd, out RECT windowRect);

                EditorPrefs.SetInt(PREF_STYLE, style);
                EditorPrefs.SetInt(PREF_RECT_L, windowRect.Left);
                EditorPrefs.SetInt(PREF_RECT_T, windowRect.Top);
                EditorPrefs.SetInt(PREF_RECT_R, windowRect.Right);
                EditorPrefs.SetInt(PREF_RECT_B, windowRect.Bottom);

                EnforceBorderlessState();
            }
            else
            {
                int style = EditorPrefs.GetInt(PREF_STYLE, GetWindowLong(hWnd, GWL_STYLE));
                SetWindowLong(hWnd, GWL_STYLE, style);

                int left = EditorPrefs.GetInt(PREF_RECT_L, 100);
                int top = EditorPrefs.GetInt(PREF_RECT_T, 100);
                int right = EditorPrefs.GetInt(PREF_RECT_R, 900);
                int bottom = EditorPrefs.GetInt(PREF_RECT_B, 700);

                SetWindowPos(hWnd, IntPtr.Zero, left, top, right - left, bottom - top, SWP_NOZORDER | SWP_FRAMECHANGED);
            }
        }

        static void EnforceBorderlessState()
        {
            var hWnd = GetMainWindowHandle();
            if (hWnd == IntPtr.Zero) 
                return;

            var monitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            var monitorInfo = new MONITORINFO
            {
                cbSize = Marshal.SizeOf(typeof(MONITORINFO))
            };
            GetMonitorInfo(monitor, ref monitorInfo);

            int width = monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left;
            int height = monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top - 1;

            int currentStyle = GetWindowLong(hWnd, GWL_STYLE);
        
            currentStyle &= ~WS_CAPTION;
            currentStyle &= ~WS_THICKFRAME;
            currentStyle &= ~WS_MINIMIZEBOX;
            currentStyle &= ~WS_MAXIMIZEBOX;
            currentStyle &= ~WS_SYSMENU;
        
            currentStyle |= WS_POPUP;

            SetWindowLong(hWnd, GWL_STYLE, currentStyle);
            SetWindowPos(hWnd, IntPtr.Zero, 
                monitorInfo.rcMonitor.Left, 
                monitorInfo.rcMonitor.Top, 
                width, 
                height, 
                SWP_NOZORDER | SWP_FRAMECHANGED);
        }
    }
}
#endif