using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MealWise.Models;
using MealWise.Services;

namespace MealWise.ViewModels;

public class ProfileViewModel : INotifyPropertyChanged
{
    private readonly ProfileService _profileService;

    public ProfileViewModel(ProfileService profileService)
    {
        _profileService = profileService;

        SelectGenderCommand = new Command<object>(OnSelectGender);
        SelectActivityLevelCommand = new Command<object>(OnSelectActivityLevel);
        SaveCommand = new Command(OnSaveProfile);
        BackCommand = new Command(OnBack);

        LoadProfile();
    }

    #region Observable Properties

    private DateTime _dateOfBirth = DateTime.Today.AddYears(-25);
    public DateTime DateOfBirth
    {
        get => _dateOfBirth;
        set => SetProperty(ref _dateOfBirth, value);
    }

    private Gender _selectedGender = Gender.Male;
    public Gender SelectedGender
    {
        get => _selectedGender;
        set
        {
            if (SetProperty(ref _selectedGender, value))
            {
                OnPropertyChanged(nameof(IsMaleSelected));
                OnPropertyChanged(nameof(IsFemaleSelected));
                OnPropertyChanged(nameof(IsOtherSelected));
            }
        }
    }

    public bool IsMaleSelected => SelectedGender == Gender.Male;
    public bool IsFemaleSelected => SelectedGender == Gender.Female;
    public bool IsOtherSelected => SelectedGender == Gender.Other;

    private double _heightCm = 178;
    public double HeightCm
    {
        get => _heightCm;
        set
        {
            if (SetProperty(ref _heightCm, value))
            {
                CalculateBmi();
            }
        }
    }

    private double _weightKg = 72;
    public double WeightKg
    {
        get => _weightKg;
        set
        {
            if (SetProperty(ref _weightKg, value))
            {
                CalculateBmi();
            }
        }
    }

    private string _dietaryRestrictions = string.Empty;
    public string DietaryRestrictions
    {
        get => _dietaryRestrictions;
        set => SetProperty(ref _dietaryRestrictions, value);
    }

    private double _bmi;
    public double Bmi
    {
        get => _bmi;
        private set => SetProperty(ref _bmi, value);
    }

    private bool _hasBmi;
    public bool HasBmi
    {
        get => _hasBmi;
        private set => SetProperty(ref _hasBmi, value);
    }

    private ActivityLevel _selectedActivityLevel = ActivityLevel.Moderate;
    public ActivityLevel SelectedActivityLevel
    {
        get => _selectedActivityLevel;
        set
        {
            if (SetProperty(ref _selectedActivityLevel, value))
            {
                OnPropertyChanged(nameof(IsSedentarySelected));
                OnPropertyChanged(nameof(IsLightSelected));
                OnPropertyChanged(nameof(IsModerateSelected));
                OnPropertyChanged(nameof(IsActiveSelected));
                OnPropertyChanged(nameof(IsVeryActiveSelected));
            }
        }
    }

    public bool IsSedentarySelected => SelectedActivityLevel == ActivityLevel.Sedentary;
    public bool IsLightSelected => SelectedActivityLevel == ActivityLevel.Light;
    public bool IsModerateSelected => SelectedActivityLevel == ActivityLevel.Moderate;
    public bool IsActiveSelected => SelectedActivityLevel == ActivityLevel.Active;
    public bool IsVeryActiveSelected => SelectedActivityLevel == ActivityLevel.VeryActive;

    #endregion

    #region Commands

    public ICommand SelectGenderCommand { get; }
    public ICommand SelectActivityLevelCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand BackCommand { get; }

    #endregion

    #region Logic Methods

    private void LoadProfile()
    {
        var profile = _profileService.GetProfile();
        if (profile != null)
        {
            DateOfBirth = profile.DateOfBirth == default ? DateTime.Today.AddYears(-25) : profile.DateOfBirth;
            SelectedGender = profile.Gender;
            HeightCm = profile.HeightCm > 0 ? profile.HeightCm : 178;
            WeightKg = profile.WeightKg > 0 ? profile.WeightKg : 72;
            DietaryRestrictions = profile.DietaryRestrictions ?? string.Empty;
            SelectedActivityLevel = profile.ActivityLevel;

            CalculateBmi();
        }
    }

    private void CalculateBmi()
    {
        if (HeightCm > 0 && WeightKg > 0)
        {
            double heightInMeters = HeightCm / 100.0;
            Bmi = Math.Round(WeightKg / (heightInMeters * heightInMeters), 1);
            HasBmi = true;
        }
        else
        {
            Bmi = 0;
            HasBmi = false;
        }
    }

    private void OnSelectGender(object? param)
    {
        if (param is Gender gender)
            SelectedGender = gender;
        else if (param is string str && Enum.TryParse<Gender>(str, true, out var parsedGender))
            SelectedGender = parsedGender;
    }

    private void OnSelectActivityLevel(object? param)
    {
        if (param is ActivityLevel level)
            SelectedActivityLevel = level;
        else if (param is string str && Enum.TryParse<ActivityLevel>(str, true, out var parsedLevel))
            SelectedActivityLevel = parsedLevel;
    }

    private void OnSaveProfile()
    {
        var profile = _profileService.GetProfile() ?? new UserProfile();

        profile.DateOfBirth = DateOfBirth;
        profile.Gender = SelectedGender;
        profile.HeightCm = HeightCm;
        profile.WeightKg = WeightKg;
        profile.DietaryRestrictions = DietaryRestrictions;
        profile.ActivityLevel = SelectedActivityLevel;

        _profileService.SaveProfile(profile);

        Shell.Current.DisplayAlert("Success", "Profile saved successfully!", "Ok");
    }

    private void OnBack()
    {
        

    }

    #endregion

    #region INotifyPropertyChanged Implementation

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}