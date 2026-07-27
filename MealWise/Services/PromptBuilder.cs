using System.Text;
using MealWise.Models;

namespace MealWise.Services;

/// <summary>
/// Helper service that constructs structured system and user prompts
/// with strict JSON output schemas for all AI interactions.
/// </summary>
public class PromptBuilder
{
    // =========================================================================
    // 1. RECIPE GENERATION PROMPTS (TAB 4)
    // =========================================================================

    /// <summary>
    /// Builds the system prompt setting up the AI as a chef/nutritionist 
    /// with strict JSON output rules for Recipe generation.
    /// </summary>
    public string BuildRecipeSystemPrompt()
    {
        return """
        You are an expert culinary chef and certified clinical nutritionist.
        Your job is to generate exactly 3 realistic, healthy, and diverse recipe suggestions based on the user's available pantry ingredients, remaining nutritional budget, and dietary preferences.

        STRICT RULES:
        1. Respond ONLY with a single valid JSON object containing an array of 3 recipes named "suggestions". Do NOT include any markdown code blocks (```json), preambles, or conversational text.
        2. Prioritize ingredients flagged as [Nearing Expiration - USE FIRST] to avoid food waste.
        3. Respect all dietary restrictions and allergies unconditionally.
        4. Provide varied options (e.g., one quick meal, one hearty meal, one light snack/soup).
        5. Tailor the nutritional values of each individual suggestion to fit comfortably within the provided remaining daily budget.

        JSON OUTPUT SCHEMA:
        {
          "suggestions": [
            {
              "title": "First Dish Name",
              "description": "Short appetizing summary (1-2 sentences)",
              "estimatedTimeMinutes": 15,
              "ingredientsUsed": [
                "100g Chicken Breast",
                "2 Tomatoes"
              ],
              "instructions": [
                "Step 1: Slice ingredients.",
                "Step 2: Stir-fry until ready."
              ],
              "nutrition": {
                "calories": 350.0,
                "proteinGrams": 28.0,
                "fatGrams": 8.0,
                "carbsGrams": 15.0,
                "fiberGrams": 3.0
              }
            },
            {
              "title": "Second Dish Name",
              "description": "Another recipe description.",
              "estimatedTimeMinutes": 25,
              "ingredientsUsed": [
                "3 Eggs",
                "50g Cheese"
              ],
              "instructions": [
                "Step 1: Beat the eggs.",
                "Step 2: Bake in oven."
              ],
              "nutrition": {
                "calories": 420.0,
                "proteinGrams": 24.0,
                "fatGrams": 18.0,
                "carbsGrams": 2.0,
                "fiberGrams": 0.0
              }
            }
          ]
        }
        """;
    }

    /// <summary>
    /// Builds the user prompt containing user profile, pantry items, remaining budget,
    /// and weekly historical trend context.
    /// </summary>
    public string BuildRecipeUserPrompt(
        UserProfile profile,
        string formattedPantryText,
        NutritionalValue remainingBudget,
        PeriodNutritionSummary? weeklySummary = null,
        string? userNote = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== USER CONTEXT ===");
        sb.AppendLine($"- Age: {profile.Age}, Gender: {profile.Gender}");
        sb.AppendLine($"- Dietary Restrictions & Allergies: {(string.IsNullOrWhiteSpace(profile.DietaryRestrictions) ? "None" : profile.DietaryRestrictions)}");

        sb.AppendLine("\n=== REMAINING DAILY NUTRITION BUDGET ===");
        sb.AppendLine($"- Target Calories Left: {remainingBudget.Calories} kcal");
        sb.AppendLine($"- Protein Left: {remainingBudget.ProteinGrams} g");
        sb.AppendLine($"- Fats Left: {remainingBudget.FatGrams} g");
        sb.AppendLine($"- Carbs Left: {remainingBudget.CarbsGrams} g");
        sb.AppendLine($"- Fiber Left: {remainingBudget.FiberGrams} g");

        if (weeklySummary != null && weeklySummary.TotalDaysAnalyzed > 0)
        {
            sb.AppendLine("\n=== 7-DAY HISTORICAL TRENDS ===");
            sb.AppendLine($"- Average Fiber Completion: {weeklySummary.AverageTargetCompletionPercentage.FiberGrams}%");
            sb.AppendLine($"- Average Protein Completion: {weeklySummary.AverageTargetCompletionPercentage.ProteinGrams}%");
            if (weeklySummary.AverageTargetCompletionPercentage.FiberGrams < 80)
            {
                sb.AppendLine("-> NOTE: The user has been low on dietary fiber over the past week. Prefer fiber-rich ingredients if available.");
            }
        }

        sb.AppendLine("\n=== PANTRY STOCK ===");
        sb.AppendLine(formattedPantryText);

        if (!string.IsNullOrWhiteSpace(userNote))
        {
            sb.AppendLine("\n=== ADDITIONAL USER REQUEST ===");
            sb.AppendLine($"User wishes: \"{userNote}\"");
        }

        sb.AppendLine("\nPlease generate a matching recipe matching the requested JSON schema.");
        return sb.ToString();
    }

    // =========================================================================
    // 2. PANTRY ITEM PARSING PROMPTS (TAB 2)
    // =========================================================================

    /// <summary>
    /// Builds system prompt for Vision or Text models to parse grocery receipts/photos into PantryItems.
    /// </summary>
    public string BuildPantryParsingSystemPrompt()
    {
        return """
            You are an automated grocery parser. Analyze the provided text or image of groceries/receipts and extract all individual raw food items.

            STRICT RULES:
            1. Return ONLY a valid JSON array of objects.
            2. Estimate nutritional values per total item quantity.
            3. You MUST assign a category string from the following enum values ONLY:
               - "MeatAndSeafood"
               - "DairyAndEggs"
               - "Produce"
               - "GrainsAndCarbs"
               - "FatsAndCondiments"
               - "SweetsAndSnacks"
               - "FrozenAndConvenience"
               - "PreparedFood"
               - "Beverages"
            4. Allowed units: "Grams", "Milliliters", "Pieces".

            JSON OUTPUT SCHEMA:
            [
              {
                "name": "Chicken Breast",
                "quantityAmount": 500.0,
                "unit": "Grams",
                "category": "MeatAndSeafood",
                "calories": 825.0,
                "proteinGrams": 155.0,
                "fatGrams": 18.0,
                "carbsGrams": 0.0,
                "fiberGrams": 0.0
              }
            ]
            """;
    }

    // =========================================================================
    // 3. MEAL LOGGING PROMPTS (TAB 3)
    // =========================================================================

    /// <summary>
    /// Builds system prompt for analyzing eaten meals (via photo or text description).
    /// </summary>
    public string BuildMealParsingSystemPrompt()
    {
        return """
            You are an expert nutritional tracking assistant. Analyze the meal description or image and estimate the dish name and total nutritional content.

            STRICT RULES:
            1. Return ONLY a single valid JSON object.
            2. Be realistic with portion sizes and hidden oils/butter in restaurant dishes.

            JSON OUTPUT SCHEMA:
            {
              "dishName": "Pepperoni Pizza (2 slices)",
              "calories": 580.0,
              "proteinGrams": 24.0,
              "fatGrams": 22.0,
              "carbsGrams": 68.0,
              "fiberGrams": 3.5
            }
            """;
    }

    // =========================================================================
    // 4. DAILY TARGET CALCULATION PROMPTS (TAB 1)
    // =========================================================================

    /// <summary>
    /// Builds system prompt to calculate personalized daily nutrition targets.
    /// </summary>
    public string BuildDailyTargetsSystemPrompt()
    {
        return """
            You are a certified sports clinical nutritionist. Calculate optimal daily calorie and macro goals for the user.

            STRICT RULES:
            1. Return ONLY a single valid JSON object.
            2. Apply medical standards (e.g., WHO guidelines) adjusted for biometric data and user goals.

            JSON OUTPUT SCHEMA:
            {
              "calories": 2200.0,
              "proteinGrams": 140.0,
              "fatGrams": 70.0,
              "carbsGrams": 250.0,
              "fiberGrams": 30.0
            }
            """;
    }
    public string BuildPantryRefinementSystemPrompt()
    {
        return """
        You are an expert grocery list refinement system.
        You are given a JSON array of current PantryItems and a user's natural language correction text.
        Your task is to modify the existing items according to the user's instructions:
        - If the user says they didn't buy an item or to remove/delete it, remove it from the list.
        - If the user corrects a quantity, unit, or name, update that specific item in the list.
        - If the user asks to add new items, parse them and add them to the JSON list following the same schema.

        STRICT RULES:
        1. Return ONLY a valid JSON array of objects. Do NOT include markdown code blocks (```json), preambles, or conversational text.
        2. Keep the exact same JSON schema for each PantryItem object:
           - "name" (string)
           - "quantityAmount" (double)
           - "unit" ("Grams", "Milliliters", "Pieces")
           - "category" (Must map to one of: "MeatAndSeafood", "DairyAndEggs", "Produce", "GrainsAndCarbs", "FatsAndCondiments", "SweetsAndSnacks", "FrozenAndConvenience", "PreparedFood", "Beverages")
           - "calories" (double)
           - "proteinGrams" (double)
           - "fatGrams" (double)
           - "carbsGrams" (double)
           - "fiberGrams" (double)
        """;
    }


}