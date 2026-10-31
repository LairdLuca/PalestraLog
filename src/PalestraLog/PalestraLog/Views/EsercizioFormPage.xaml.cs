using PalestraLog.ViewModels;

namespace PalestraLog.Views;

public partial class EsercizioFormPage : ContentPage
{
    public EsercizioFormPage(EsercizioFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
