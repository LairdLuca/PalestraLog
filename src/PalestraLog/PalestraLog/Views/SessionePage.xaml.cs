using PalestraLog.ViewModels;

namespace PalestraLog.Views;

public partial class SessionePage : ContentPage
{
    public SessionePage(SessioneViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // NB: niente Dispose del VM in OnDisappearing.
    // SessioneViewModel è registrato Singleton: il suo stato (timer, set
    // inseriti, peso/reps correnti) deve sopravvivere allo smontaggio della
    // pagina così che, rientrando, l'esercizio "in corso" sia esattamente
    // come l'utente lo aveva lasciato.
}
