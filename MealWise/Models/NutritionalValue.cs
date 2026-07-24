using System;
using System.Collections.Generic;
using System.Text;

namespace MealWise;

public class NutritionalValue
{
    public double Calories { get; set; }
    public double ProteinGrams { get; set; }
    public double FatGrams { get; set; }
    public double CarbsGrams { get; set; }
    public double FiberGrams { get; set; }

    // Operator overload to easily sum up daily nutrition
    // e.g., totalMacros = meal1.Nutrition + meal2.Nutrition;
    public static NutritionalValue operator +(NutritionalValue a, NutritionalValue b)
    {
        return new NutritionalValue
        {
            Calories = a.Calories + b.Calories,
            ProteinGrams = a.ProteinGrams + b.ProteinGrams,
            FatGrams = a.FatGrams + b.FatGrams,
            CarbsGrams = a.CarbsGrams + b.CarbsGrams,
            FiberGrams = a.FiberGrams + b.FiberGrams
        };
    }
}
