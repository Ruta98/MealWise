using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MealWise.Models;
using MealWise.Services;
using Microsoft.Maui.Controls;

namespace MealWise.ViewModels;

public class DiaryViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _dbService;
    private readonly AiService _aiService;
    private readonly ProfileService _profileService;
    private readonly NutritionCalculator _nutritionCalculator;
    private readonly MediaService _mediaService;

    private MealEntry? _pendingMeal;

    // Зображення
    private ImageSource? _selectedImageSource;
    public ImageSource? SelectedImageSource
    {
        get => _selectedImageSource;
        set { _selectedImageSource = value; OnPropertyChanged(); }
    }

    private byte[]? _optimizedImageBytes;
    public byte[]? OptimizedImageBytes
    {
        get => _optimizedImageBytes;
        set { _optimizedImageBytes = value; OnPropertyChanged(); }
    }

    private bool _hasSelectedImage;
    public bool HasSelectedImage
    {
        get => _hasSelectedImage;
        set { _hasSelectedImage = value; OnPropertyChanged(); }
    }

    private bool _isAnalyzing;
    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set { _isAnalyzing = value; OnPropertyChanged(); }
    }

    private string _statusText = "Recognizing your meal...";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    private string _recognizedFoodName = "Analyzing photo...";
    public string RecognizedFoodName
    {
        get => _recognizedFoodName;
        set { _recognizedFoodName = value; OnPropertyChanged(); }
    }

    private int _recognizedCalories;
    public int RecognizedCalories
    {
        get => _recognizedCalories;
        set { _recognizedCalories = value; OnPropertyChanged(); }
    }

    public string SelectedDateText => DateTime.Today.ToString("dd.MM.yyyy");

    #region Nutrition Metrics

    public int CaloriesCurrent { get; set; }
    public int CaloriesGoal { get; set; } = 2000;
    public double CaloriesProgress => CaloriesGoal > 0 ? (double)CaloriesCurrent / CaloriesGoal : 0;

    public int ProteinCurrent { get; set; }
    public int ProteinGoal { get; set; } = 100;
    public double ProteinProgress => ProteinGoal > 0 ? (double)ProteinCurrent / ProteinGoal : 0;

    public int CarbsCurrent { get; set; }
    public int CarbsGoal { get; set; } = 200;
    public double CarbsProgress => CarbsGoal > 0 ? (double)CarbsCurrent / CarbsGoal : 0;

    public int FatCurrent { get; set; }
    public int FatGoal { get; set; } = 70;
    public double FatProgress => FatGoal > 0 ? (double)FatCurrent / FatGoal : 0;

    public int TotalProgressPercentage => (int)Math.Clamp(CaloriesProgress * 100, 0, 100);

    #endregion

    // Список записів за день (обгортка для UI)
    public ObservableCollection<MealLogItem> TodayLogs { get; set; } = new();

    // Команди
    public ICommand SelectPhotoSourceCommand { get; }
    public ICommand ConfirmMealCommand { get; }
    public ICommand SaveToDiaryCommand { get; }
    public ICommand AddWeightCommand { get; }
    public ICommand AddTextCommand { get; }

    public DiaryViewModel(
        DatabaseService dbService,
        AiService aiService,
        ProfileService profileService,
        NutritionCalculator nutritionCalculator)
    {
        _dbService = dbService;
        _aiService = aiService;
        _profileService = profileService;
        _nutritionCalculator = nutritionCalculator;
        _mediaService = new MediaService();

        SelectPhotoSourceCommand = new Command(async () => await ShowPhotoSourceOptions());
        ConfirmMealCommand = new Command(async () => await ConfirmMealAsync());
        SaveToDiaryCommand = new Command(async () => await SaveToDiaryAsync());
        AddWeightCommand = new Command(async () => await OnAddWeightAsync());
        AddTextCommand = new Command(async () => await OnAddTextAsync());

        // Завантаження актуальних даних КБЖВ
        _ = RefreshNutritionAsync();
    }

    private async Task RefreshNutritionAsync()
    {
        try
        {
            var profile = _profileService.GetProfile();
            var target = profile.DailyTargetNutrition;

            // Встановлюємо добові цілі з профілю
            CaloriesGoal = (int)target.Calories;
            ProteinGoal = (int)target.ProteinGrams;
            CarbsGoal = (int)target.CarbsGrams;
            FatGoal = (int)target.FatGrams;

            // Завантажуємо спожиті сьогодні страви з SQLite
            var todayMeals = await _dbService.GetDailyMealsAsync(DateTime.Today);
            var consumed = _nutritionCalculator.SumDailyMeals(todayMeals);

            // Оновлюємо фактичні показники
            CaloriesCurrent = (int)consumed.Calories;
            ProteinCurrent = (int)consumed.ProteinGrams;
            CarbsCurrent = (int)consumed.CarbsGrams;
            FatCurrent = (int)consumed.FatGrams;

            // Оновлюємо список відображення страв
            TodayLogs.Clear();
            foreach (var meal in todayMeals)
            {
                TodayLogs.Add(new MealLogItem
                {
                    MealType = GetMealTypeByTime(meal.DateConsumed),
                    Name = meal.DishName,
                    Calories = (int)meal.Calories
                });
            }

            // Оновлюємо властивості в UI
            OnPropertyChanged(nameof(CaloriesCurrent));
            OnPropertyChanged(nameof(CaloriesGoal));
            OnPropertyChanged(nameof(CaloriesProgress));
            OnPropertyChanged(nameof(ProteinCurrent));
            OnPropertyChanged(nameof(ProteinGoal));
            OnPropertyChanged(nameof(ProteinProgress));
            OnPropertyChanged(nameof(CarbsCurrent));
            OnPropertyChanged(nameof(CarbsGoal));
            OnPropertyChanged(nameof(CarbsProgress));
            OnPropertyChanged(nameof(FatCurrent));
            OnPropertyChanged(nameof(FatGoal));
            OnPropertyChanged(nameof(FatProgress));
            OnPropertyChanged(nameof(TotalProgressPercentage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing daily nutrition: {ex.Message}");
        }
    }

    private string GetMealTypeByTime(DateTime time)
    {
        int hour = time.Hour;
        if (hour >= 5 && hour < 11) return "Breakfast";
        if (hour >= 11 && hour < 16) return "Lunch";
        if (hour >= 16 && hour < 22) return "Dinner";
        return "Snack";
    }

    private async Task ShowPhotoSourceOptions()
    {
        string action = await Shell.Current.DisplayActionSheet("Select photo source", "Cancel", null, "Take Photo", "Choose from Gallery");

        if (action == "Take Photo")
        {
            var bytes = await _mediaService.TakePhotoAsync();
            _ = ProcessSelectedPhotoAsync(bytes);
        }
        else if (action == "Choose from Gallery")
        {
            var bytes = await _mediaService.PickPhotoAsync();
            _ = ProcessSelectedPhotoAsync(bytes);
        }
    }

    private async Task ProcessSelectedPhotoAsync(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return;

        OptimizedImageBytes = bytes;
        SelectedImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
        HasSelectedImage = true;

        IsAnalyzing = true;
        StatusText = "Recognizing your meal...";
        RecognizedFoodName = "Analyzing photo with AI...";
        RecognizedCalories = 0;

        try
        {
            var parsedMeal = await _aiService.RecognizeMealFromPhotoAsync(bytes);
            if (parsedMeal != null)
            {
                _pendingMeal = parsedMeal;
                _pendingMeal.DateConsumed = DateTime.Now;

                RecognizedFoodName = _pendingMeal.DishName;
                RecognizedCalories = (int)_pendingMeal.Calories;
                StatusText = "Meal recognized!";
            }
            else
            {
                HasSelectedImage = false;
                await Shell.Current.DisplayAlert("AI Vision", "Failed to identify the food. Please try a clearer picture.", "OK");
            }
        }
        catch (Exception ex)
        {
            HasSelectedImage = false;
            await Shell.Current.DisplayAlert("AI Error", $"Could not process meal image: {ex.Message}", "OK");
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private async Task OnAddTextAsync()
    {
        string result = await Shell.Current.DisplayPromptAsync(
            "Log Meal",
            "What did you consume? (e.g. '1 large banana and 2 boiled eggs'):",
            "Parse with AI",
            "Cancel");

        if (string.IsNullOrWhiteSpace(result)) return;

        IsAnalyzing = true;
        HasSelectedImage = true;
        SelectedImageSource = null; // Немає зображення для текстового введення
        StatusText = "Interpreting text with AI...";
        RecognizedFoodName = "Analyzing description...";
        RecognizedCalories = 0;

        try
        {
            var parsedMeal = await _aiService.ParseMealFromTextAsync(result);
            if (parsedMeal != null)
            {
                _pendingMeal = parsedMeal;
                _pendingMeal.DateConsumed = DateTime.Now;

                RecognizedFoodName = _pendingMeal.DishName;
                RecognizedCalories = (int)_pendingMeal.Calories;
                StatusText = "Meal parsed!";
            }
            else
            {
                HasSelectedImage = false;
                await Shell.Current.DisplayAlert("AI Parsing", "Could not understand the input. Try listing ingredients.", "OK");
            }
        }
        catch (Exception ex)
        {
            HasSelectedImage = false;
            await Shell.Current.DisplayAlert("AI Error", $"Parsing failed: {ex.Message}", "OK");
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private async Task OnAddWeightAsync()
    {
        string result = await Shell.Current.DisplayPromptAsync(
            "Update Weight",
            "Enter your actual weight in kilograms:",
            "Update",
            "Cancel",
            "70",
            maxLength: 5,
            keyboard: Keyboard.Numeric);

        if (double.TryParse(result, out double newWeight) && newWeight > 0)
        {
            var profile = _profileService.GetProfile();
            profile.WeightKg = newWeight;

            // Оновлюємо цілі відповідно до нової ваги
            profile.DailyTargetNutrition = _nutritionCalculator.CalculateDailyTarget(profile);
            _profileService.SaveProfile(profile);

            await RefreshNutritionAsync();
            await Shell.Current.DisplayAlert("Profile Updated", $"New weight ({newWeight} kg) updated. Your target nutrition is refreshed!", "OK");
        }
    }

    private async Task ConfirmMealAsync()
    {
        if (_pendingMeal == null) return;

        try
        {
            // Зберігаємо страву в локальну базу даних SQLite
            await _dbService.SaveMealEntryAsync(_pendingMeal);

            // Скидаємо стан блоку розпізнавання
            HasSelectedImage = false;
            SelectedImageSource = null;
            OptimizedImageBytes = null;
            _pendingMeal = null;

            // Перераховуємо поточне КБЖВ
            await RefreshNutritionAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Database Error", $"Could not save meal: {ex.Message}", "OK");
        }
    }

    private async Task SaveToDiaryAsync()
    {
        // Дані вже автоматично та надійно зберігаються при підтвердженні ("Confirm") кожної страви.
        await Shell.Current.DisplayAlert("Success", "All records have been saved securely in your daily diary database.", "OK");
    }

    #region PropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

public class MealLogItem
{
    public string MealType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Calories { get; set; }
}