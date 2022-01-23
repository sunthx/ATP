using System.Windows;

namespace ATP.Themes.Controls
{
    public class TheWindow : Window
    {
        public TheWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TheWindow),new FrameworkPropertyMetadata(typeof(TheWindow)));
        }
    }
}
