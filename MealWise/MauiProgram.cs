using Microsoft.Extensions.Logging;
using MealWise.Services;
using MealWise.ViewModels;
using MealWise.Views;

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
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<ProfileService>();

        // =========================================================
        // 2. DOMAIN & HELPER SERVICES (SINGLETONS)
        // =========================================================
        builder.Services.AddSingleton<PromptBuilder>();
        builder.Services.AddSingleton<PantryFilterService>();
        builder.Services.AddSingleton<NutritionCalculator>();

        // =========================================================
        // 3. HTTP & AI ORCHESTRATOR SERVICE
        // =========================================================
        builder.Services.AddHttpClient<AiService>();

        // =========================================================
        // 4. VIEW MODELS (TRANSIENTS) — ОСЬ ТУТ ⬇️
        // =========================================================
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<PantryViewModel>();
        builder.Services.AddTransient<DiaryViewModel>();
        builder.Services.AddTransient<RecipeViewModel>();

        // =========================================================
        // 5. PAGES / VIEWS (TRANSIENTS) — І ТУТ ⬇️
        // =========================================================
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<PantryPage>();
        builder.Services.AddTransient<DiaryPage>();
        builder.Services.AddTransient<RecipePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Фінальна збірка контейнера
        return builder.Build();
    }
}