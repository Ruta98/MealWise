using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MealWise.Models;

public class MealEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string DishName { get; set; } = string.Empty;
    public DateTime DateConsumed { get; set; } = DateTime.Now;

    // --- SQLite Flat Columns ---
    public double Calories { get; set; }
    public double ProteinGrams { get; set; }
    public double FatGrams { get; set; }
    public double CarbsGrams { get; set; }
    public double FiberGrams { get; set; }

    // --- C# / AI Convenience Property ---
    [Ignore]
    public NutritionalValue Nutrition
    {
        get => new NutritionalValue
        {
            Calories = this.Calories,
            ProteinGrams = this.ProteinGrams,
            FatGrams = this.FatGrams,
            CarbsGrams = this.CarbsGrams,
            FiberGrams = this.FiberGrams
        };
        set
        {
            Calories = value.Calories;
            ProteinGrams = value.ProteinGrams;
            FatGrams = value.FatGrams;
            CarbsGrams = value.CarbsGrams;
            FiberGrams = value.FiberGrams;
        }
    }
}