using MealWise.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MealWise.Services;

public class ProfileService
{
    // Unique key to store the JSON string in app preferences
    private const string ProfileKey = "User_Profile_Data";

    /// <summary>
    /// Retrieves the user profile from local preferences.
    /// If no profile exists, returns a new default profile.
    /// </summary>
    public UserProfile GetProfile()
    {
        // 1. Try to get the saved JSON string
        string savedJson = Preferences.Default.Get(ProfileKey, string.Empty);

        // 2. If it's empty (e.g., first app launch), return a default profile
        if (string.IsNullOrEmpty(savedJson))
        {
            return new UserProfile();
        }

        try
        {
            // 3. Deserialize JSON back into a UserProfile object
            return JsonSerializer.Deserialize<UserProfile>(savedJson) ?? new UserProfile();
        }
        catch (JsonException)
        {
            // Fallback in case the saved JSON got corrupted
            return new UserProfile();
        }
    }

    /// <summary>
    /// Serializes and saves the user profile to local preferences.
    /// </summary>
    public void SaveProfile(UserProfile profile)
    {
        if (profile == null)
            return;

        // 1. Convert the C# object to a JSON string
        string json = JsonSerializer.Serialize(profile);

        // 2. Save the string to Preferences
        Preferences.Default.Set(ProfileKey, json);
    }

    /// <summary>
    /// Clears the saved profile (useful for a "Reset" or "Log out" button).
    /// </summary>
    public void ClearProfile()
    {
        Preferences.Default.Remove(ProfileKey);
    }
}