using System;
using System.Collections.Generic;
using System.Text;

namespace MealWise.Models;

public class UserProfile
{
    public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-25);
    public double WeightKg { get; set; }
    public double HeightCm { get; set; }

    public Gender Gender { get; set; } = Gender.Male;
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Moderate;

    // Single free-text field for AI parsing (e.g., "Vegan, allergic to peanuts")
    public string DietaryRestrictions { get; set; } = string.Empty;

    // The daily target calculated by AI or basic formulas based on the profile
    public NutritionalValue DailyTargetNutrition { get; set; } = new NutritionalValue();

    // Helper property to calculate age for the AI system prompt
    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
