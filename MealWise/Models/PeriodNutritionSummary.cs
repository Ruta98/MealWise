namespace MealWise.Models;

/// <summary>
/// Aggregated nutrition statistics over a specific timeframe (e.g., 7 or 30 days).
/// </summary>
public class PeriodNutritionSummary
{
    public int TotalDaysAnalyzed { get; set; }
    public NutritionalValue TotalConsumed { get; set; } = new();
    public NutritionalValue DailyAverage { get; set; } = new();

    /// <summary>
    /// Percentage of daily target achieved on average (e.g., 95.0 = 95%).
    /// </summary>
    public NutritionalValue AverageTargetCompletionPercentage { get; set; } = new();
}