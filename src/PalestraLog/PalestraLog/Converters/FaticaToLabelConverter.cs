using System.Globalization;

namespace PalestraLog.Converters;

public class FaticaToLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fatica = value switch
        {
            int i => i,
            null => 0,
            _ => 0
        };

        return fatica switch
        {
            1 => "troppo leggero",
            2 => "leggero",
            3 => "giusto",
            4 => "pesante",
            5 => "al limite",
            _ => string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
