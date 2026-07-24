using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MealWise.Models;

namespace MealWise.Services;

/// <summary>
/// Service responsible for persisting and retrieving user profile data 
/// using MAUI Preferences, and notifying the app when biometrics change.
/// </summary>
public class ProfileService
{
    private const string ProfileKey = "User_Profile_Data";

    /// <summary>
    /// Event triggered whenever the user profile is saved or updated.
    /// ViewModels can subscribe to this event to refresh nutrition targets in real time.
    /// </summary>
    public event EventHandler<UserProfile>? ProfileChanged;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Checks whether the user has saved a profile at least once.
    /// Useful for onboarding navigation checks.
    /// </summary>
    public bool IsProfileConfigured => Preferences.Default.ContainsKey(ProfileKey);

    /// <summary>
    /// Retrieves the user profile from local preferences.
    /// Returns a default profile if none exists.
    /// </summary>
    public UserProfile GetProfile()
    {
        string savedJson = Preferences.Default.Get(ProfileKey, string.Empty);

        if (string.IsNullOrWhiteSpace(savedJson))
        {
            return new UserProfile();
        }

        try
        {
            return JsonSerializer.Deserialize<UserProfile>(savedJson, _jsonOptions) ?? new UserProfile();
        }
        catch (JsonException)
        {
            // Fallback in case stored JSON was corrupted
            return new UserProfile();
        }
    }

    /// <summary>
    /// Serializes and saves the user profile, triggering the ProfileChanged event.
    /// </summary>
    public void SaveProfile(UserProfile profile)
    {
        if (profile == null)
            return;

        string json = JsonSerializer.Serialize(profile, _jsonOptions);
        Preferences.Default.Set(ProfileKey, json);

        // Notify listening ViewModels about the updated profile data
        ProfileChanged?.Invoke(this, profile);
    }

    /// <summary>
    /// Clears stored profile data and notifies listeners.
    /// </summary>
    public void ClearProfile()
    {
        Preferences.Default.Remove(ProfileKey);
        ProfileChanged?.Invoke(this, new UserProfile());
    }
}