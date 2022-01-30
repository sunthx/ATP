using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace ATP.Internal
{
    internal class NativeMethods
    {
        /// <summary>
        /// 还原窗口
        /// </summary>
        public static void BringWindowToFront(uint curProcessId, IntPtr wnd)
        {
            var hForeWnd = User32.GetForegroundWindow();
            uint currentProcessId = (uint)curProcessId;

            User32.GetWindowThreadProcessId(hForeWnd, out var processId);
            User32.AttachThreadInput(currentProcessId, processId, true);
            User32.SetForegroundWindow(wnd);
            User32.ShowWindow(wnd, ShowWindowCommand.SW_RESTORE);
            User32.SetWindowPos(wnd, HWND.HWND_TOPMOST, 0, 0, 0, 0,
                User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
            User32.SetWindowPos(wnd, HWND.HWND_NOTOPMOST, 0, 0, 0, 0,
                User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
            User32.SetForegroundWindow(wnd);
            User32.AttachThreadInput(currentProcessId, processId, false);
        }

        /// <summary>
        /// 提取并保存应用程序的图标文件@
        /// </summary>            
        public static void ExtractAndSaveAppIconFile(string appExecuteFilePath, string saveFilePath)
        {
            Shell32.SHFILEINFO shinfo = new Shell32.SHFILEINFO();

            IntPtr hImgSmall = Vanara.PInvoke.Shell32.SHGetFileInfo(
                appExecuteFilePath,
                0,
                ref shinfo,
                (int)Marshal.SizeOf(shinfo),
                Shell32.SHGFI.SHGFI_LARGEICON | Shell32.SHGFI.SHGFI_ICON);

            shinfo.hIcon.ToIcon().ToBitmap().Save(saveFilePath);
            User32.DestroyIcon(shinfo.hIcon);
        }

        /// <summary>
        /// 注册一个全局的键盘Hook
        /// </summary>
        public static User32.SafeHHOOK RegisterKeyboardHook(User32.HookProc hookProcedure)
        {
            var moduleHandle =
                Kernel32.GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule?.ModuleName);

            return User32.SetWindowsHookEx(
                User32.HookType.WH_KEYBOARD_LL,
                hookProcedure,
                moduleHandle,
                0);
        }

        /// <summary>
        /// 释放一个Hook
        /// </summary>
        public static bool ReleaseWindowsHook(User32.HHOOK hook)
        {
            return User32.UnhookWindowsHookEx(hook);
        }
    }
}