using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MealWise.Models;
using MealWise.Services;
using Microsoft.Maui.Controls;

namespace MealWise.ViewModels;

public class PantryViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _dbService;
    private readonly AiService _aiService;
    private readonly MediaService _mediaService;

    // Список продуктів у коморі
    public ObservableCollection<PantryItem> PantryItems { get; set; } = new();

    private bool _isInterpreting;
    public bool IsInterpreting
    {
        get => _isInterpreting;
        set { _isInterpreting = value; OnPropertyChanged(); }
    }

    private string _statusTitle = "Interpreting your input";
    public string StatusTitle
    {
        get => _statusTitle;
        set { _statusTitle = value; OnPropertyChanged(); }
    }

    private string _statusSubtitle = "We'll turn this into ingredients automatically";
    public string StatusSubtitle
    {
        get => _statusSubtitle;
        set { _statusSubtitle = value; OnPropertyChanged(); }
    }
    // 1. Додайте нову властивість для тексту зворотного зв'язку
    private string _discrepancyText = string.Empty;
    public string DiscrepancyText
    {
        get => _discrepancyText;
        set { _discrepancyText = value; OnPropertyChanged(); }
    }

    private bool _isReviewingDraft;
    public bool IsReviewingDraft
    {
        get => _isReviewingDraft;
        set { _isReviewingDraft = value; OnPropertyChanged(); }
    }

    // 2. Додайте нову команду
    public ICommand RefineWithAiCommand { get; }

    // 3. Ініціалізуйте її у конструкторі:
    // RefineWithAiCommand = new Command(async () => await OnRefineWithAiAsync());

    // 4. Реалізуйте метод OnRefineWithAiAsync:
    private async Task OnRefineWithAiAsync()
    {
        if (string.IsNullOrWhiteSpace(DiscrepancyText)) return;

        IsInterpreting = true;
        StatusTitle = "Refining ingredients list...";
        StatusSubtitle = "Shaping the list to match your corrections";

        try
        {
            var currentItemsList = PantryItems.ToList();

            // Відправляємо поточний стан + коментар користувача до ШІ
            var refinedItems = await _aiService.RefinePantryItemsAsync(currentItemsList, DiscrepancyText);

            if (refinedItems != null && refinedItems.Any())
            {
                PantryItems.Clear();
                foreach (var item in refinedItems)
                {
                    PantryItems.Add(item);
                }

                DiscrepancyText = string.Empty; // Очищуємо поле після успішного оновлення
            }
            else
            {
                await Shell.Current.DisplayAlert("AI Refinement", "Could not apply corrections. Please check the text and try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Refinement process failed: {ex.Message}", "OK");
        }
        finally
        {
            IsInterpreting = false;
        }
    }
    // Команди
    public ICommand BackCommand { get; }
    public ICommand TakePhotoCommand { get; }
    public ICommand TypeInputCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveToKitchenCommand { get; }

    public PantryViewModel(DatabaseService dbService, AiService aiService)
    {
        _dbService = dbService;
        _aiService = aiService;
        _mediaService = new MediaService();

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        TakePhotoCommand = new Command(async () => await OnTakePhotoAsync());
        TypeInputCommand = new Command(async () => await OnTypeInputAsync());
        RemoveItemCommand = new Command<PantryItem>(OnRemoveItem);
        SaveToKitchenCommand = new Command(async () => await OnSaveToKitchenAsync());

        // Завантажуємо існуючі продукти
        _ = LoadDatabaseItemsAsync();
    }

    private async Task LoadDatabaseItemsAsync()
    {
        try
        {
            var dbItems = await _dbService.GetPantryItemsAsync();
            PantryItems.Clear();
            foreach (var item in dbItems)
            {
                PantryItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading database items: {ex.Message}");
        }
    }

    private async Task OnTakePhotoAsync()
    {
        string action = await Shell.Current.DisplayActionSheet(
            "Select photo source", "Cancel", null, "Take Photo", "Choose from Gallery");

        byte[]? photoBytes = null;

        if (action == "Take Photo")
        {
            photoBytes = await _mediaService.TakePhotoAsync();
        }
        else if (action == "Choose from Gallery")
        {
            photoBytes = await _mediaService.PickPhotoAsync();
        }

        if (photoBytes == null || photoBytes.Length == 0) return;

        IsInterpreting = true;
        StatusTitle = "Analyzing your photo...";
        StatusSubtitle = "Extracting food items and portion sizes";

        try
        {
            var parsedItems = await _aiService.RecognizePantryFromPhotoAsync(photoBytes);
            if (parsedItems != null && parsedItems.Any())
            {
                foreach (var item in parsedItems)
                {
                    PantryItems.Add(item);
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("AI Parsing", "No recognized food items found. Try another photo.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not process image: {ex.Message}", "OK");
        }
        finally
        {
            IsInterpreting = false;
        }
    }

    private async Task OnTypeInputAsync()
    {
        string result = await Shell.Current.DisplayPromptAsync(
            "Type Ingredients",
            "List what you bought or have in fridge (e.g. 6 eggs, 1l milk, 4 carrots):",
            "Parse with AI",
            "Cancel");

        if (string.IsNullOrWhiteSpace(result)) return;

        IsInterpreting = true;
        StatusTitle = "Interpreting your input";
        StatusSubtitle = "We'll turn this into ingredients automatically";

        try
        {
            var parsedItems = await _aiService.ParsePantryFromTextAsync(result);
            if (parsedItems != null && parsedItems.Any())
            {
                foreach (var item in parsedItems)
                {
                    PantryItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Parsing failed: {ex.Message}", "OK");
        }
        finally
        {
            IsInterpreting = false;
        }
    }

    private void OnRemoveItem(PantryItem? item)
    {
        if (item != null && PantryItems.Contains(item))
        {
            PantryItems.Remove(item);
        }
    }

    private async Task OnSaveToKitchenAsync()
    {
        try
        {
            // Перезаписуємо SQLite БД актуальним складом продуктів
            await _dbService.ClearPantryAsync();
            await _dbService.SavePantryItemsBatchAsync(PantryItems);

            await Shell.Current.DisplayAlert("Kitchen Updated", "Your pantry stock has been saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not update kitchen database: {ex.Message}", "OK");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}