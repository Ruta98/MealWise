using System.Text;
using MealWise.Models;

namespace MealWise.Services;

/// <summary>
/// Helper service responsible for smart filtering, balancing, 
/// and formatting pantry items to create optimal AI context windows.
/// </summary>
public class PantryFilterService
{
    /// <summary>
    /// Selects a balanced subset of cooking ingredients from total pantry stock.
    /// Excludes non-cooking categories (snacks, drinks, ready meals) and balances
    /// fresh perishables, staples, and condiments.
    /// </summary>
    /// <param name="allItems">Complete list of items retrieved from SQLite</param>
    /// <param name="maxPerishables">Maximum number of fresh meat, produce, and dairy items</param>
    /// <param name="maxCarbs">Maximum number of grains, pasta, and bread items</param>
    /// <param name="maxCondiments">Maximum number of oils, spices, and sauces</param>
    public List<PantryItem> GetBalancedPantrySelection(
        List<PantryItem> allItems,
        int maxPerishables = 8,
        int maxCarbs = 3,
        int maxCondiments = 4)
    {
        if (allItems == null || !allItems.Any())
            return new List<PantryItem>();

        // 1. Filter out items with 0 quantity and non-cooking categories
        var cookingEligibleItems = allItems
            .Where(item => item.QuantityAmount > 0)
            .Where(item => item.Category != ProductCategory.SweetsAndSnacks &&
                           item.Category != ProductCategory.Beverages &&
                           item.Category != ProductCategory.PreparedFood)
            .ToList();

        // 2. Select fresh perishable items (Meat, Produce, Dairy) - highest usage priority
        var perishables = cookingEligibleItems
            .Where(item => item.Category == ProductCategory.MeatAndSeafood ||
                           item.Category == ProductCategory.Produce ||
                           item.Category == ProductCategory.DairyAndEggs ||
                           item.Category == ProductCategory.FrozenAndConvenience)
            .OrderByDescending(item => item.AddedDate)
            .Take(maxPerishables)
            .ToList();

        // 3. Select staple carbohydrates (Grains, Pasta, Flour)
        var carbs = cookingEligibleItems
            .Where(item => item.Category == ProductCategory.GrainsAndCarbs)
            .OrderByDescending(item => item.AddedDate)
            .Take(maxCarbs)
            .ToList();

        // 4. Select condiments, cooking fats, and spices
        var condiments = cookingEligibleItems
            .Where(item => item.Category == ProductCategory.FatsAndCondiments)
            .Take(maxCondiments)
            .ToList();

        // 5. Merge into a distinct balanced list
        return perishables
            .Concat(carbs)
            .Concat(condiments)
            .DistinctBy(item => item.Id)
            .ToList();
    }

    /// <summary>
    /// Formats a list of filtered PantryItems into a clean, human-readable 
    /// text block grouped by category for the AI Prompt.
    /// </summary>
    public string FormatPantryForPrompt(List<PantryItem> items)
    {
        if (items == null || !items.Any())
            return "No specific ingredients available in pantry. Suggest a budget recipe with generic basic ingredients.";

        var sb = new StringBuilder();
        sb.AppendLine("Available Pantry Ingredients:");

        // Group items by category to give structural context to the LLM
        var grouped = items.GroupBy(i => GetCategoryGroupHeader(i.Category));

        foreach (var group in grouped)
        {
            sb.AppendLine($"\n[{group.Key}]:");
            foreach (var item in group)
            {
                sb.AppendLine($"- {item.Name}: {item.QuantityAmount} {item.Unit}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Maps ProductCategory enum values to clear, descriptive section headers for the LLM.
    /// </summary>
    private string GetCategoryGroupHeader(ProductCategory category) => category switch
    {
        ProductCategory.MeatAndSeafood => "Proteins & Seafood",
        ProductCategory.DairyAndEggs => "Dairy & Eggs",
        ProductCategory.Produce => "Fresh Produce & Vegetables (Priority)",
        ProductCategory.GrainsAndCarbs => "Carbohydrates & Staples",
        ProductCategory.FatsAndCondiments => "Oils, Spices & Condiments",
        ProductCategory.FrozenAndConvenience => "Frozen Base Items",
        _ => "Other Ingredients"
    };
}