using PalestraLog.ViewModels;

namespace PalestraLog.Helpers;

public class BloccoStoricoTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SingoloTemplate { get; set; }
    public DataTemplate? GruppoTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container) => item switch
    {
        BloccoStoricoSingolo => SingoloTemplate,
        BloccoStoricoGruppo => GruppoTemplate,
        _ => null
    };
}
