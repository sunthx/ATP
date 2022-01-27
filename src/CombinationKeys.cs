using System.Text;
using System.Windows.Forms;

namespace ATP
{
    /// <summary>
    /// 表示一个组合键
    /// </summary>
    /// <remarks>
    /// 修饰键 + 字母或者数字 
    /// </remarks>
    public class CombinationKeys
    {
        public CombinationKeys(Keys key, KeyDirection keyDirection, bool altPressed, bool controlPressed, bool shiftPressed)
        {
            AltPressed = altPressed;
            ControlPressed = controlPressed;
            Key = key;
            KeyDirection = keyDirection;
            ShiftPressed = shiftPressed;
        }

        public bool AltPressed { get; private set; }
        public bool ControlPressed { get; private set; }
        public bool ShiftPressed { get; private set; }
        public Keys Key { get; private set; }
        public KeyDirection KeyDirection { get; private set; }

        public bool IsValid()
        {
            return  IsModifierKeyPressed() && IsCommonKeyPressed();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (ControlPressed)
                sb.Append("Ctrl + ");
            if (ShiftPressed)
                sb.Append("Shift + ");
            if (AltPressed)
                sb.Append("Alt + ");

            var keyString = IsNumber() ? Key.ToString().Remove(0, 1) : Key.ToString();
            sb.Append(keyString);

            return sb.ToString();
        }

        public bool IsLetter()
        {
            return Key >= Keys.A && Key <= Keys.Z;
        }

        public bool IsNumber()
        {
            return Key >= Keys.D0 && Key <= Keys.D9;
        }

        public bool IsAnyKeyPressed()
        {
            return IsModifierKeyPressed() || IsCommonKeyPressed();
        }

        private bool IsCommonKeyPressed()
        {
            return (IsLetter() || IsNumber()) && KeyDirection == KeyDirection.Down;
        }

        private bool IsModifierKeyPressed()
        {
            return (AltPressed || ControlPressed || ShiftPressed);
        }
    }
}