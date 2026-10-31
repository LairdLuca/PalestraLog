using System.Globalization;

namespace PalestraLog.Converters;

public class FaticaToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int fatica)
        {
            return fatica switch
            {
                1 => Colors.Green,
                2 => Colors.LightGreen,
                3 => Colors.Gold,
                4 => Colors.Orange,
                5 => Colors.Red,
                _ => Colors.Gray
            };
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
