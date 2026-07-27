using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using MealWise.Models;

namespace MealWise.Converters;

public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProductCategory category)
        {
            return category switch
            {
                ProductCategory.MeatAndSeafood => "🥩",
                ProductCategory.DairyAndEggs => "🥛",
                ProductCategory.Produce => "🥦",
                ProductCategory.GrainsAndCarbs => "🌾",
                ProductCategory.FatsAndCondiments => "🥑",
                ProductCategory.SweetsAndSnacks => "🍫",
                ProductCategory.FrozenAndConvenience => "🧊",
                ProductCategory.PreparedFood => "🍲",
                ProductCategory.Beverages => "🥤",
                _ => "🍏"
            };
        }
        return "🍏";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}