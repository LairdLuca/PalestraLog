using PalestraLog.ViewModels;

namespace PalestraLog.Views;

public partial class EsercizioListPage : ContentPage
{
    private readonly EsercizioListViewModel _viewModel;

    public EsercizioListPage(EsercizioListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadEserciziCommand.Execute(null);
    }
}
