using System.Globalization;

namespace PalestraLog.Converters;

public class BoolToWarmupBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isWarmup = value is bool b && b;
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        return isWarmup
            ? Color.FromArgb(isDark ? "#1F3D38" : "#E0F4F1")
            : Color.FromArgb(isDark ? "#212121" : "#E1E1E1");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
