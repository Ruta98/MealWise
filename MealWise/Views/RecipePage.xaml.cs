using MealWise.ViewModels;

namespace MealWise.Views;

public partial class RecipePage : ContentPage
{
    public RecipePage(RecipeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}