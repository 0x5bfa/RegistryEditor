using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RegistryValley.App.Converters
{
    internal class ColorToSolidColorBrushConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (null == value)
                return null;

            if (value is Color color)
                return new SolidColorBrush(color);

            Type type = value.GetType();
            throw new InvalidOperationException($"Unsupported type [{type.Name}]");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
