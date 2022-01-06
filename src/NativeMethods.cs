using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Vanara.PInvoke;

namespace AltTabPlus
{
    internal class NativeMethods
    {
        /// <summary>
        /// 还原窗口
        /// </summary>
        /// <param name="name"></param>
        public static void BringWindowToFront(string name)
        {
            var wnd = User32.FindWindow(null, name);
            User32.ShowWindow(wnd, ShowWindowCommand.SW_NORMAL);
            User32.SetForegroundWindow(wnd);
        }

        /// <summary>
        /// 提取并保存应用程序的图标文件
        /// </summary>            
        public static void ExtractAndSaveAppIconFile(string appExecuteFilePath,string saveFilePath)
        {
            Shell32.SHFILEINFO shinfo = new Shell32.SHFILEINFO();

            IntPtr hImgSmall = Vanara.PInvoke.Shell32.SHGetFileInfo(
                appExecuteFilePath,
                0,
                ref shinfo,
                (int)Marshal.SizeOf(shinfo),
                Shell32.SHGFI.SHGFI_LARGEICON | Shell32.SHGFI.SHGFI_ICON);

            shinfo.hIcon.ToIcon().ToBitmap().Save(saveFilePath);
            Vanara.PInvoke.User32.DestroyIcon(shinfo.hIcon);
        }

        /// <summary>
        /// 注册一个全局的键盘Hook
        /// </summary>
        public static User32.HHOOK RegisterKeyboardHook(User32.HookProc hookProcedure)
        {
            var moduleHandle =
                Kernel32.GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName);

            var globalKeyboardHook = User32.SetWindowsHookEx(
                User32.HookType.WH_KEYBOARD_LL,
                hookProcedure, 
                moduleHandle,
                0);

            return globalKeyboardHook;
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
