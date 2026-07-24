using MealWise.ViewModels;

namespace MealWise.Views;

public partial class DiaryPage : ContentPage
{
    public DiaryPage(DiaryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}