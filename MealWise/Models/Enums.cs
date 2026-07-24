using System;
using System.Collections.Generic;
using System.Text;

namespace MealWise.Models;

public enum Gender
{
    Male,
    Female,
    Other
}

public enum ActivityLevel
{
    Sedentary = 1,   // Little to no exercise
    Light = 2,       // Light exercise 1-3 days/week
    Moderate = 3,    // Moderate exercise 3-5 days/week
    Active = 4,      // Heavy exercise 6-7 days/week
    VeryActive = 5   // Very heavy exercise, physical job
}

public enum Unit
{
    Grams,       // "g"
    Milliliters, // "ml"
    Pieces       // "pcs"
}