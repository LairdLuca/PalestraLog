using PalestraLog.ViewModels;

namespace PalestraLog.Helpers;

public class BloccoListaTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SingoloTemplate { get; set; }
    public DataTemplate? GruppoTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container) => item switch
    {
        BloccoSingolo => SingoloTemplate,
        BloccoGruppo => GruppoTemplate,
        _ => null
    };
}
