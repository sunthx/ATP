using System.Windows;
using System.Windows.Input;

namespace ATP.Themes.Controls
{
    public class TheWindow : Window
    {
        public TheWindow()
        {
            DefaultStyleKey = typeof(TheWindow);

            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, this.MinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, this.CloseWindow));
        }

        #region Title Bar

        public static readonly DependencyProperty TitleBarHeightProperty = DependencyProperty.Register(
            "TitleBarHeight", typeof(double), typeof(TheWindow), new PropertyMetadata(SystemParameters.WindowNonClientFrameThickness.Top));

        public double TitleBarHeight
        {
            get { return (double)GetValue(TitleBarHeightProperty); }
            set { SetValue(TitleBarHeightProperty, value); }
        }

        public static readonly DependencyProperty TitleFontSizeProperty = DependencyProperty.Register(
            "TitleFontSize", typeof(double), typeof(TheWindow), new PropertyMetadata(SystemFonts.CaptionFontSize));

        public double TitleFontSize
        {
            get { return (double)GetValue(TitleFontSizeProperty); }
            set { SetValue(TitleFontSizeProperty, value); }
        }

        #region Icon

        public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register(
            "IconHeight", typeof(double), typeof(TheWindow), new PropertyMetadata(SystemParameters.SmallIconHeight));

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register(
            "IconWidth", typeof(double), typeof(TheWindow), new PropertyMetadata(SystemParameters.SmallIconWidth));

        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        #endregion


        #endregion

        #region WindowCommands

        private void MinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void CloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }



        #endregion
    }
}
