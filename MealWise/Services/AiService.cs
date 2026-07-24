using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MealWise.Models;

namespace MealWise.Services;

/// <summary>
/// Fully implemented AI service orchestrating DeepSeek / Vision API integration.
/// Handles recipes generation, text parsing, and multimodal image recognition.
/// </summary>
public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly PantryFilterService _pantryFilter;

    // API Configuration constants
    private const string DeepSeekApiKey = "YOUR_DEEPSEEK_API_KEY"; // Replace with your actual key
    private const string ApiBaseUrl = "https://api.deepseek.com/v1/chat/completions";
    private const string DefaultModel = "deepseek-chat";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AiService(
        HttpClient httpClient,
        PromptBuilder promptBuilder,
        PantryFilterService pantryFilter)
    {
        _httpClient = httpClient;
        _promptBuilder = promptBuilder;
        _pantryFilter = pantryFilter;

        // Configure default HTTP headers for API authentication
        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", DeepSeekApiKey);
        }
    }

    // =========================================================================
    // 1. RECIPE GENERATION (TAB 4)
    // =========================================================================

    /// <summary>
    /// Generates a personalized recipe using DeepSeek LLM based on user constraints,
    /// available pantry selection, remaining nutrition budget, and historical trends.
    /// </summary>
    public async Task<Recipe?> GenerateRecipeAsync(
        UserProfile profile,
        List<PantryItem> availablePantry,
        NutritionalValue remainingNutrition,
        PeriodNutritionSummary? weeklySummary = null,
        string? userCustomPrompt = null)
    {
        // 1. Filter and balance raw pantry stock (e.g. Max 15 items: Fresh + Carbs + Spices)
        var balancedPantry = _pantryFilter.GetBalancedPantrySelection(availablePantry);
        string formattedPantryText = _pantryFilter.FormatPantryForPrompt(balancedPantry);

        // 2. Build system and user prompts
        string systemPrompt = _promptBuilder.BuildRecipeSystemPrompt();
        string userPrompt = _promptBuilder.BuildRecipeUserPrompt(
            profile,
            formattedPantryText,
            remainingNutrition,
            weeklySummary,
            userCustomPrompt);

        // 3. Call LLM API and parse result
        string jsonResponse = await SendChatCompletionAsync(systemPrompt, userPrompt);
        return DeserializeResponse<Recipe>(jsonResponse);
    }

    // =========================================================================
    // 2. PANTRY INGREDIENTS ENTRY (TAB 2)
    // =========================================================================

    /// <summary>
    /// Parses natural text describing purchased products into a structured list of PantryItems.
    /// </summary>
    public async Task<List<PantryItem>> ParsePantryFromTextAsync(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return new List<PantryItem>();

        string systemPrompt = _promptBuilder.BuildPantryParsingSystemPrompt();
        string userPrompt = $"Parse the following grocery text into structured pantry items:\n\"{rawInput}\"";

        string jsonResponse = await SendChatCompletionAsync(systemPrompt, userPrompt);
        return DeserializeResponse<List<PantryItem>>(jsonResponse) ?? new List<PantryItem>();
    }

    /// <summary>
    /// Analyzes an image of groceries/receipt via Vision API and returns recognized items.
    /// </summary>
    public async Task<List<PantryItem>> RecognizePantryFromPhotoAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return new List<PantryItem>();

        string systemPrompt = _promptBuilder.BuildPantryParsingSystemPrompt();
        string base64Image = Convert.ToBase64String(imageBytes);

        string jsonResponse = await SendVisionCompletionAsync(
            systemPrompt,
            "Analyze this grocery image and return all detected ingredients with their estimated quantities and categories.",
            base64Image);

        return DeserializeResponse<List<PantryItem>>(jsonResponse) ?? new List<PantryItem>();
    }

    // =========================================================================
    // 3. MEAL LOGGING & TRACKING (TAB 3)
    // =========================================================================

    /// <summary>
    /// Parses natural text describing a meal into a structured MealEntry object.
    /// </summary>
    public async Task<MealEntry?> ParseMealFromTextAsync(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return null;

        string systemPrompt = _promptBuilder.BuildMealParsingSystemPrompt();
        string userPrompt = $"Analyze this consumed meal and estimate its calories and macros:\n\"{rawText}\"";

        string jsonResponse = await SendChatCompletionAsync(systemPrompt, userPrompt);
        return DeserializeResponse<MealEntry>(jsonResponse);
    }

    /// <summary>
    /// Analyzes a photo of a prepared dish via Vision API to estimate portion size and nutrition.
    /// </summary>
    public async Task<MealEntry?> RecognizeMealFromPhotoAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return null;

        string systemPrompt = _promptBuilder.BuildMealParsingSystemPrompt();
        string base64Image = Convert.ToBase64String(imageBytes);

        string jsonResponse = await SendVisionCompletionAsync(
            systemPrompt,
            "Identify this meal and estimate its total portion calories and macronutrients.",
            base64Image);

        return DeserializeResponse<MealEntry>(jsonResponse);
    }

    // =========================================================================
    // 4. PROFILE & NUTRITION TARGETS (TAB 1)
    // =========================================================================

    /// <summary>
    /// Uses AI nutritionist logic to calculate custom daily targets for a user profile.
    /// </summary>
    public async Task<NutritionalValue?> CalculateDailyTargetsAsync(UserProfile profile)
    {
        string systemPrompt = _promptBuilder.BuildDailyTargetsSystemPrompt();
        string userPrompt = $"""
            Calculate nutrition targets for:
            - Age: {profile.Age}
            - Gender: {profile.Gender}
            - Weight: {profile.WeightKg} kg
            - Height: {profile.HeightCm} cm
            - Activity Level: {profile.ActivityLevel}
            """;

        string jsonResponse = await SendChatCompletionAsync(systemPrompt, userPrompt);
        return DeserializeResponse<NutritionalValue>(jsonResponse);
    }

    // =========================================================================
    // PRIVATE HTTP & INFRASTRUCTURE HELPERS
    // =========================================================================

    /// <summary>
    /// Sends a standard text-based chat completion request to the OpenAI-compatible REST API.
    /// </summary>
    private async Task<string> SendChatCompletionAsync(string systemPrompt, string userPrompt)
    {
        var requestBody = new
        {
            model = DefaultModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3, // Low temperature for deterministic, accurate JSON responses
            response_format = new { type = "json_object" }
        };

        return await PostApiRequestAsync(requestBody);
    }

    /// <summary>
    /// Sends a multimodal Vision request containing a Base64 image payload.
    /// </summary>
    private async Task<string> SendVisionCompletionAsync(string systemPrompt, string userPrompt, string base64Image)
    {
        var requestBody = new
        {
            model = "gpt-4o-mini", // Fallback vision model endpoint
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userPrompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                        }
                    }
                }
            },
            max_tokens = 1000
        };

        return await PostApiRequestAsync(requestBody);
    }

    /// <summary>
    /// Core HTTP POST helper executing API call and handling response streams.
    /// </summary>
    private async Task<string> PostApiRequestAsync(object payload)
    {
        string jsonPayload = JsonSerializer.Serialize(payload);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(ApiBaseUrl, content);
        response.EnsureSuccessStatusCode();

        string rawResponseJson = await response.Content.ReadAsStringAsync();

        // Extract content text from OpenAI API choices structure
        using var doc = JsonDocument.Parse(rawResponseJson);
        string messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return CleanJsonResponse(messageContent);
    }

    /// <summary>
    /// Sanitizes the AI response string by stripping Markdown code fences if present.
    /// </summary>
    private static string CleanJsonResponse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        string cleaned = rawText.Trim();

        // Strip markdown code block wrappers (```json ... ```)
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..];
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned[3..];
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned[..^3];
        }

        return cleaned.Trim();
    }

    /// <summary>
    /// Safely deserializes raw JSON into C# models with error handling.
    /// </summary>
    private T? DeserializeResponse<T>(string cleanJson) where T : class
    {
        if (string.IsNullOrWhiteSpace(cleanJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(cleanJson, _jsonOptions);
        }
        catch (JsonException)
        {
            // Log exception or handle deserialization failure
            return null;
        }
    }
}