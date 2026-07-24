using MealWise.Services;
using MealWise.Services.NutriSnap.Services;
using Microsoft.Extensions.Logging;
//using MealWise.ViewModels;
//using MealWise.Views;

namespace MealWise;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // =========================================================
        // 1. DATA & STORAGE SERVICES (SINGLETONS)
        // =========================================================
        // Singletons live for the entire lifetime of the application.

        // SQLite async connection manager
        builder.Services.AddSingleton<DatabaseService>();

        // Local user preferences manager
        builder.Services.AddSingleton<ProfileService>();

        // =========================================================
        // 2. DOMAIN & HELPER SERVICES (SINGLETONS)
        // =========================================================
        // Stateless calculation and prompt builder services.

        builder.Services.AddSingleton<PromptBuilder>();
        builder.Services.AddSingleton<PantryFilterService>();
        builder.Services.AddSingleton<NutritionCalculator>();

        // =========================================================
        // 3. HTTP & AI ORCHESTRATOR SERVICE
        // =========================================================
        // AddHttpClient automatically injects a managed HttpClient into AiService,
        // and resolves PromptBuilder & PantryFilterService from DI container.

        builder.Services.AddHttpClient<AiService>();

        // =========================================================
        // 4. VIEW MODELS & PAGES (TRANSIENTS)
        // =========================================================
        // Transients are recreated every time the user navigates to the page.

        // builder.Services.AddTransient<RecipeViewModel>();
        // builder.Services.AddTransient<RecipePage>();
        // builder.Services.AddTransient<PantryViewModel>();
        // builder.Services.AddTransient<PantryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}