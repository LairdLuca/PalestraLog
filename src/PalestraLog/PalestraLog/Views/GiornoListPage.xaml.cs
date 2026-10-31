using PalestraLog.ViewModels;

namespace PalestraLog.Views;

public partial class GiornoListPage : ContentPage
{
    public GiornoListPage(GiornoListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
