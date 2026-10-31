using PalestraLog.ViewModels;

namespace PalestraLog.Views;

public partial class SchedaFormPage : ContentPage
{
    public SchedaFormPage(SchedaFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
