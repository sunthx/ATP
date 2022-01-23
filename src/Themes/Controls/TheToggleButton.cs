using System.Windows;
using System.Windows.Controls.Primitives;

namespace ATP.Themes.Controls
{
    public class TheToggleButton : ToggleButton
    {
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            "CornerRadius", typeof(CornerRadius), typeof(TheToggleButton), new PropertyMetadata(default(CornerRadius)));

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        static TheToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TheToggleButton),new FrameworkPropertyMetadata(typeof(TheToggleButton)));
        }
    }
}
