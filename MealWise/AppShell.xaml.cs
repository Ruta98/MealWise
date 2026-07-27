namespace MealWise
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("RecipeDetailsPage", typeof(Views.RecipeDetailsPage));
        }
    }
}
