using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.PInvoke;

namespace ATP.Internal
{
    public class KeyboardService
    {
        private readonly User32.HookProc _keyboardHookProc;
        private User32.SafeHHOOK _globalKeyboardHook;
        private bool _isContinueWhenHandled = true;

        public KeyboardService()
        {
            _keyboardHookProc = KeyboardHookProc;
        }

        public event Action<CombinationKeys> OnReceived; 

        public void Start()
        {
            _globalKeyboardHook = NativeMethods.RegisterKeyboardHook(_keyboardHookProc);
        }

        public void Stop()
        {
            NativeMethods.ReleaseWindowsHook(_globalKeyboardHook);
        }

        private IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            var alt = (Control.ModifierKeys & Keys.Alt) != 0;
            var control = (Control.ModifierKeys & Keys.Control) != 0;
            var shift = (Control.ModifierKeys & Keys.Shift) != 0;
            var keyDown = wParam == (IntPtr)User32.WindowMessage.WM_KEYDOWN;
            var keyUp = wParam == (IntPtr)User32.WindowMessage.WM_KEYUP;
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            //http://msdn.microsoft.com/en-us/library/windows/desktop/ms646286(v=vs.85).aspx
            if (key != Keys.RMenu && key != Keys.LMenu && wParam == (IntPtr)User32.WindowMessage.WM_SYSKEYDOWN)
            {
                alt = true;
                keyDown = true;
            }

            if (key != Keys.RMenu && key != Keys.LMenu && wParam == (IntPtr)User32.WindowMessage.WM_SYSKEYUP)
            {
                alt = true;
                keyUp = true;
            }

            var combinationKeys = new CombinationKeys(
                key,
                keyDown
                    ? KeyDirection.Down
                    : keyUp
                        ? KeyDirection.Up
                        : KeyDirection.Unknown,
                alt, control, shift);

            var isValid = combinationKeys.IsValid();
            if (!isValid) 
                return User32.CallNextHookEx(_globalKeyboardHook, code, wParam, lParam);

            OnReceived?.Invoke(combinationKeys);
            if (combinationKeys.IsHandled && !_isContinueWhenHandled)
                return (IntPtr)1;

            return User32.CallNextHookEx(_globalKeyboardHook, code, wParam, lParam);
        }
    }
}