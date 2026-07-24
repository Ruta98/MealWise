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

public enum ProductCategory
{
    // --- BASIC & COOKING INGREDIENTS ---

    /// <summary> Fresh meat, poultry, and fish </summary>
    MeatAndSeafood = 1,

    /// <summary> Eggs, cottage cheese, milk, cheese, yogurts </summary>
    DairyAndEggs = 2,

    /// <summary> Fresh vegetables, greens, mushrooms, and fruits </summary>
    Produce = 3,

    /// <summary> Grains, pasta, flour, bread, legumes </summary>
    GrainsAndCarbs = 4,

    /// <summary> Oils, nuts, seeds, butter, spices, sauces, condiments </summary>
    FatsAndCondiments = 5,

    // --- PROCESSED, SNACKS & CONVENIENCE ---

    /// <summary> Chips, crackers, chocolate, cookies, sweets </summary>
    SweetsAndSnacks = 6,

    /// <summary> Frozen dumplings, nuggets, semi-finished products </summary>
    FrozenAndConvenience = 7,

    /// <summary> Fast food, takeout meals, deli dishes </summary>
    PreparedFood = 8,

    /// <summary> Sodas, juices, alcohol, sweet drinks </summary>
    Beverages = 9
}