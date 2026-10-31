using PalestraLog.ViewModels;

namespace PalestraLog.Views;

public partial class StoricoSessioneDettaglioPage : ContentPage
{
    public StoricoSessioneDettaglioPage(StoricoSessioneDettaglioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
