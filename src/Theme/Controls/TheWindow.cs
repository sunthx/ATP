using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ATP.Theme.Controls
{
    public class TheWindow : Window
    {
        public TheWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TheWindow),new FrameworkPropertyMetadata(typeof(TheWindow)));
        }
    }
}
