using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AltTabPlus;

public class BitmapImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == default || string.IsNullOrEmpty(value.ToString()))
            return default;

        var filePath = value.ToString();
        if (!File.Exists(filePath))
            return default;
        
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(filePath, UriKind.Absolute);
        image.EndInit();

        return image;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}