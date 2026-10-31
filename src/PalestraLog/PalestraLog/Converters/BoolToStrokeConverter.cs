using System.Globalization;

namespace PalestraLog.Converters;

public class BoolToStrokeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
            return Application.Current?.Resources.TryGetValue("Primary", out var color) == true ? color : Colors.Blue;
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
