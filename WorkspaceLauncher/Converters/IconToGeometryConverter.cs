using System;
using System.Globalization;
using System.Windows.Data;
using WorkspaceLauncher.Helpers;

namespace WorkspaceLauncher.Converters
{
    public class IconToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string iconKey)
            {
                return IconHelper.GetIconGeometry(iconKey);
            }
            return IconHelper.GetIconGeometry("Briefcase");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
