using System.Globalization;

namespace PalestraLog.Converters;

/// <summary>
/// Converte un bool nel colore "alert" HUD (rosso) se true,
/// altrimenti nel colore di accent ambra. Usato per evidenziare
/// stati di "oltre limite" (es. set extra oltre il target di scheda).
/// </summary>
public class BoolToHudAlertColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isAlert = value is bool b && b;
        if (isAlert) return Color.FromArgb("#FF3B30");

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return isDark ? Color.FromArgb("#FFB800") : Color.FromArgb("#B8860B");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
