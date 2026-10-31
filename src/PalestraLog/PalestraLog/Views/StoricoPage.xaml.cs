using PalestraLog.ViewModels;

namespace PalestraLog.Views;

public partial class StoricoPage : ContentPage
{
    private readonly StoricoViewModel _viewModel;

    public StoricoPage(StoricoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadStoricoCommand.Execute(null);
    }
}
