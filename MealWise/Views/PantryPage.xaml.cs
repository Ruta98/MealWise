using MealWise.ViewModels;

namespace MealWise.Views;

public partial class PantryPage : ContentPage
{
    public PantryPage(PantryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}