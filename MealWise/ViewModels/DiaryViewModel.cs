using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MealWise.Services;

namespace MealWise.ViewModels;

public class DiaryViewModel : INotifyPropertyChanged
{
    private readonly MediaService _mediaService;

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

    // Метрики харчування
    public int CaloriesCurrent { get; set; } = 1560;
    public int CaloriesGoal { get; set; } = 2500;
    public double CaloriesProgress => (double)CaloriesCurrent / CaloriesGoal;

    public int ProteinCurrent { get; set; } = 78;
    public int ProteinGoal { get; set; } = 120;
    public double ProteinProgress => (double)ProteinCurrent / ProteinGoal;

    public int CarbsCurrent { get; set; } = 180;
    public int CarbsGoal { get; set; } = 2500;
    public double CarbsProgress => (double)CarbsCurrent / CarbsGoal;

    public int FatCurrent { get; set; } = 55;
    public int FatGoal { get; set; } = 80;
    public double FatProgress => (double)FatCurrent / FatGoal;

    public int TotalProgressPercentage => (int)(CaloriesProgress * 100);

    // Список записів за день
    public ObservableCollection<MealLogItem> TodayLogs { get; set; } = new();

    // Команди
    public ICommand SelectPhotoSourceCommand { get; }
    public ICommand ConfirmMealCommand { get; }
    public ICommand SaveToDiaryCommand { get; }

    public DiaryViewModel()
    {
        _mediaService = new MediaService();

        SelectPhotoSourceCommand = new Command(async () => await ShowPhotoSourceOptions());
        ConfirmMealCommand = new Command(ConfirmMeal);
        SaveToDiaryCommand = new Command(SaveToDiary);
    }

    private async Task ShowPhotoSourceOptions()
    {
        string action = await Shell.Current.DisplayActionSheet("Select photo source", "Cancel", null, "Take Photo", "Choose from Gallery");

        if (action == "Take Photo")
        {
            var bytes = await _mediaService.TakePhotoAsync();
            ProcessSelectedPhoto(bytes);
        }
        else if (action == "Choose from Gallery")
        {
            var bytes = await _mediaService.PickPhotoAsync();
            ProcessSelectedPhoto(bytes);
        }
    }

    private async void ProcessSelectedPhoto(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return;

        OptimizedImageBytes = bytes;
        SelectedImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
        HasSelectedImage = true;

        // Імітація процесу розпізнавання штучним інтелектом
        IsAnalyzing = true;
        StatusText = "Recognizing your meal...";
        RecognizedFoodName = "Analyzing image...";
        RecognizedCalories = 0;

        await Task.Delay(1500); // Імітація запиту до AI API

        IsAnalyzing = false;
        StatusText = "Meal recognized!";
        RecognizedFoodName = "Grilled Chicken Bowl";
        RecognizedCalories = 420;
    }

    private void ConfirmMeal()
    {
        if (string.IsNullOrEmpty(RecognizedFoodName)) return;

        TodayLogs.Add(new MealLogItem
        {
            MealType = "Lunch",
            Name = RecognizedFoodName,
            Calories = RecognizedCalories
        });

        // Скидаємо стан блоку розпізнавання
        HasSelectedImage = false;
        SelectedImageSource = null;
        OptimizedImageBytes = null;
    }

    private void SaveToDiary()
    {
        // Збереження даних у БД / локальне сховище
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class MealLogItem
{
    public string MealType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Calories { get; set; }
}