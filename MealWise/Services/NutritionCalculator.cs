using MealWise.Models;

namespace MealWise.Services;

/// <summary>
/// Calculates nutritional targets, remaining daily budgets, 
/// and historical trends based on WHO (World Health Organization) standards.
/// </summary>
public class NutritionCalculator
{
    // =========================================================================
    // 1. DAILY TARGET CALCULATION (WHO / Mifflin-St Jeor Standards)
    // =========================================================================

    /// <summary>
    /// Calculates recommended daily nutritional targets based on biometric user data.
    /// Uses Mifflin-St Jeor equation for BMR and WHO physical activity levels (PAL).
    /// </summary>
    public NutritionalValue CalculateDailyTarget(UserProfile profile)
    {
        if (profile == null) return new NutritionalValue();

        // Step 1: Calculate Basal Metabolic Rate (BMR) using Mifflin-St Jeor formula
        double bmr;
        if (profile.Gender == Gender.Male)
        {
            bmr = (10 * profile.WeightKg) + (6.25 * profile.HeightCm) - (5 * profile.Age) + 5;
        }
        else // Female / Other baseline
        {
            bmr = (10 * profile.WeightKg) + (6.25 * profile.HeightCm) - (5 * profile.Age) - 161;
        }

        // Step 2: Calculate Total Daily Energy Expenditure (TDEE) via Activity Multipliers
        double activityMultiplier = profile.ActivityLevel switch
        {
            ActivityLevel.Sedentary => 1.2,
            ActivityLevel.Light => 1.375,
            ActivityLevel.Moderate => 1.55,
            ActivityLevel.Active => 1.725,
            ActivityLevel.VeryActive => 1.9,
            _ => 1.2
        };

        double tdeeCalories = Math.Round(bmr * activityMultiplier);

        // Step 3: WHO Macro Percentages Split:
        // - Protein: 15% of daily calories (4 kcal per gram)
        // - Fats: 30% of daily calories (WHO upper limit recommendation, 9 kcal per gram)
        // - Carbohydrates: 55% of daily calories (4 kcal per gram)
        // - Dietary Fiber: WHO recommendation ~14g per 1000 kcal (minimum 25g/day for adults)

        double proteinGrams = Math.Round((tdeeCalories * 0.15) / 4.0, 1);
        double fatGrams = Math.Round((tdeeCalories * 0.30) / 9.0, 1);
        double carbsGrams = Math.Round((tdeeCalories * 0.55) / 4.0, 1);
        double fiberGrams = Math.Round(Math.Max(25.0, (tdeeCalories / 1000.0) * 14.0), 1);

        return new NutritionalValue
        {
            Calories = tdeeCalories,
            ProteinGrams = proteinGrams,
            FatGrams = fatGrams,
            CarbsGrams = carbsGrams,
            FiberGrams = fiberGrams
        };
    }

    // =========================================================================
    // 2. DAILY REMAINING BUDGET
    // =========================================================================

    /// <summary>
    /// Sums up all meal entries for a single day.
    /// </summary>
    public NutritionalValue SumDailyMeals(List<MealEntry> todayMeals)
    {
        if (todayMeals == null || !todayMeals.Any())
            return new NutritionalValue();

        var total = new NutritionalValue();
        foreach (var meal in todayMeals)
        {
            total += meal.Nutrition;
        }
        return total;
    }

    /// <summary>
    /// Calculates the remaining nutritional budget for today (Target - Consumed).
    /// Values can be negative if the user exceeded their daily target.
    /// </summary>
    public NutritionalValue CalculateRemainingDailyBudget(NutritionalValue target, List<MealEntry> todayMeals)
    {
        var consumed = SumDailyMeals(todayMeals);

        return new NutritionalValue
        {
            Calories = Math.Round(target.Calories - consumed.Calories, 1),
            ProteinGrams = Math.Round(target.ProteinGrams - consumed.ProteinGrams, 1),
            FatGrams = Math.Round(target.FatGrams - consumed.FatGrams, 1),
            CarbsGrams = Math.Round(target.CarbsGrams - consumed.CarbsGrams, 1),
            FiberGrams = Math.Round(target.FiberGrams - consumed.FiberGrams, 1)
        };
    }

    // =========================================================================
    // 3. HISTORICAL PERIOD ANALYSIS (7 Days / 30 Days)
    // =========================================================================

    /// <summary>
    /// Calculates aggregated metrics over a given historical period (e.g., 7 or 30 days).
    /// Used for trend analysis and supplying long-term health context to AI prompts.
    /// </summary>
    /// <param name="periodMeals">All meal entries within the timeframe</param>
    /// <param name="dailyTarget">User's current daily target</param>
    /// <param name="daysInPeriod">Number of days in period (e.g. 7 for week, 30 for month)</param>
    public PeriodNutritionSummary CalculatePeriodSummary(
        List<MealEntry> periodMeals,
        NutritionalValue dailyTarget,
        int daysInPeriod)
    {
        if (daysInPeriod <= 0) daysInPeriod = 1;

        var totalConsumed = SumDailyMeals(periodMeals);

        // Daily Averages over the period
        var dailyAverage = new NutritionalValue
        {
            Calories = Math.Round(totalConsumed.Calories / daysInPeriod, 1),
            ProteinGrams = Math.Round(totalConsumed.ProteinGrams / daysInPeriod, 1),
            FatGrams = Math.Round(totalConsumed.FatGrams / daysInPeriod, 1),
            CarbsGrams = Math.Round(totalConsumed.CarbsGrams / daysInPeriod, 1),
            FiberGrams = Math.Round(totalConsumed.FiberGrams / daysInPeriod, 1)
        };

        // Percentage of daily target achieved (e.g., 100% means perfect compliance)
        var completionPercentage = new NutritionalValue
        {
            Calories = dailyTarget.Calories > 0 ? Math.Round((dailyAverage.Calories / dailyTarget.Calories) * 100, 1) : 0,
            ProteinGrams = dailyTarget.ProteinGrams > 0 ? Math.Round((dailyAverage.ProteinGrams / dailyTarget.ProteinGrams) * 100, 1) : 0,
            FatGrams = dailyTarget.FatGrams > 0 ? Math.Round((dailyAverage.FatGrams / dailyTarget.FatGrams) * 100, 1) : 0,
            CarbsGrams = dailyTarget.CarbsGrams > 0 ? Math.Round((dailyAverage.CarbsGrams / dailyTarget.CarbsGrams) * 100, 1) : 0,
            FiberGrams = dailyTarget.FiberGrams > 0 ? Math.Round((dailyAverage.FiberGrams / dailyTarget.FiberGrams) * 100, 1) : 0
        };

        return new PeriodNutritionSummary
        {
            TotalDaysAnalyzed = daysInPeriod,
            TotalConsumed = totalConsumed,
            DailyAverage = dailyAverage,
            AverageTargetCompletionPercentage = completionPercentage
        };
    }
}