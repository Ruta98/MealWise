using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using MealWise.Models;

namespace MealWise.Converters;

public class UnitToAbbreviationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Unit unit)
        {
            return unit switch
            {
                Unit.Grams => "g",
                Unit.Milliliters => "ml",
                Unit.Pieces => "pcs",
                _ => string.Empty
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}