using System.Globalization;

namespace PalestraLog.Converters;

public class BoolToPlayPauseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool isRunning && isRunning ? "⏸" : "▶";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
