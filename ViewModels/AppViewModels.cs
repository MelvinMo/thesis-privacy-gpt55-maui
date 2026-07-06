using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepTrackerMaui.Models;
using SleepTrackerMaui.Repositories;
using SleepTrackerMaui.Services;
using SleepTrackerMaui.Stores;

namespace SleepTrackerMaui.ViewModels;

public sealed partial class AuthViewModel(IAuthRepository authRepository) : ObservableObject
{
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private bool registerMode;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public bool IsAuthenticated => authRepository.IsAuthenticated;

    public async Task CheckAuthAsync()
    {
        await authRepository.CheckAuthAsync();
        OnPropertyChanged(nameof(IsAuthenticated));
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            if (RegisterMode)
            {
                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Passwords do not match";
                    return;
                }
                await authRepository.RegisterAsync(FirstName, LastName, Email, Password);
            }
            else
            {
                await authRepository.LoginAsync(Email, Password);
            }
            OnPropertyChanged(nameof(IsAuthenticated));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleMode()
    {
        RegisterMode = !RegisterMode;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await authRepository.LogoutAsync();
        OnPropertyChanged(nameof(IsAuthenticated));
    }
}

public sealed partial class ProfileViewModel(
    IProfileRepository profileRepository,
    IDeviceSensorService sensors,
    IForegroundAccelerometerController foregroundAccelerometerController) : ObservableObject
{
    [ObservableProperty] private UserConsentPreferences preferences = UserConsentPreferences.Default;
    [ObservableProperty] private bool privacyOnboardingComplete;
    [ObservableProperty] private bool appOnboardingComplete;

    public async Task LoadAsync()
    {
        Preferences = await profileRepository.GetPreferencesAsync();
        PrivacyOnboardingComplete = profileRepository.PrivacyOnboardingComplete;
        AppOnboardingComplete = profileRepository.AppOnboardingComplete;
    }

    public async Task SavePreferencesAsync(UserConsentPreferences value)
    {
        Preferences = value;
        await profileRepository.SavePreferencesAsync(value);
    }

    public async Task SetAccelerometerEnabledAsync(bool enabled)
    {
        Preferences = Preferences with { AccelerometerEnabled = enabled };
        await profileRepository.SavePreferencesAsync(Preferences);
        if (enabled)
        {
            foregroundAccelerometerController.Start();
            await sensors.StartAccelerometerAsync();
        }
        else
        {
            await sensors.StopAccelerometerAsync();
            foregroundAccelerometerController.Stop();
        }
    }

    public async Task SetMicrophoneEnabledAsync(bool enabled)
    {
        Preferences = Preferences with { MicrophoneEnabled = enabled };
        await profileRepository.SavePreferencesAsync(Preferences);
    }

    public async Task SetLightSensorEnabledAsync(bool enabled)
    {
        Preferences = Preferences with { LightSensorEnabled = enabled };
        await profileRepository.SavePreferencesAsync(Preferences);
    }

    public async Task SetCloudStorageEnabledAsync(bool enabled)
    {
        Preferences = Preferences with { CloudStorageEnabled = enabled };
        await profileRepository.SavePreferencesAsync(Preferences);
    }

    public async Task SetPrivacyPolicyAgreementAsync(bool enabled)
    {
        Preferences = Preferences with { AgreedToPrivacyPolicy = enabled };
        await profileRepository.SavePreferencesAsync(Preferences);
    }

    public void MarkPrivacyComplete()
    {
        profileRepository.PrivacyOnboardingComplete = true;
        PrivacyOnboardingComplete = true;
    }

    public void MarkAppComplete()
    {
        profileRepository.AppOnboardingComplete = true;
        AppOnboardingComplete = true;
    }
}

public sealed partial class SleepViewModel(IJournalRepository journalRepository) : ObservableObject
{
    [ObservableProperty] private string bedtime = string.Empty;
    [ObservableProperty] private string alarm = string.Empty;
    [ObservableProperty] private bool sleepModeActive;
    [ObservableProperty] private string errorMessage = string.Empty;

    public async Task LoadAsync()
    {
        JournalData? journal = await journalRepository.GetJournalByDateAsync(DateTime.Today.ToString("yyyy-MM-dd"));
        Bedtime = journal?.Bedtime ?? string.Empty;
        Alarm = journal?.AlarmTime ?? string.Empty;
    }

    public async Task SaveBedtimeAsync(string value)
    {
        Bedtime = value;
        await journalRepository.EditJournalAsync(DateTime.Today.ToString("yyyy-MM-dd"), new JournalPatch(Date: DateTime.Today.ToString("yyyy-MM-dd"), Bedtime: value));
    }

    public async Task SaveAlarmAsync(string value)
    {
        Alarm = value;
        await journalRepository.EditJournalAsync(DateTime.Today.ToString("yyyy-MM-dd"), new JournalPatch(Date: DateTime.Today.ToString("yyyy-MM-dd"), AlarmTime: value));
    }
}

public sealed partial class JournalViewModel(IJournalRepository journalRepository) : ObservableObject
{
    [ObservableProperty] private string selectedDate = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string bedtime = string.Empty;
    [ObservableProperty] private string alarm = string.Empty;
    [ObservableProperty] private string sleepDuration = string.Empty;
    [ObservableProperty] private string diaryEntry = string.Empty;
    [ObservableProperty] private string sleepGoal = "8h 30m";
    [ObservableProperty] private string errorMessage = string.Empty;

    // MIGRATION: RN used a typed SleepNote union. MAUI exposes it through an
    //            ObservableCollection so chips update immediately in the page.
    public ObservableCollection<SleepNote> SleepNotes { get; } = [];

    public async Task LoadAsync(string date)
    {
        SelectedDate = date;
        JournalData? journal = await journalRepository.GetJournalByDateAsync(date);
        Bedtime = journal?.Bedtime ?? string.Empty;
        Alarm = journal?.AlarmTime ?? string.Empty;
        SleepDuration = journal?.SleepDuration ?? string.Empty;
        DiaryEntry = journal?.DiaryEntry ?? string.Empty;
        SleepNotes.Clear();
        foreach (SleepNote note in journal?.SleepNotes ?? Array.Empty<SleepNote>())
        {
            SleepNotes.Add(note);
        }
    }

    public async Task SaveDiaryAsync(string value)
    {
        DiaryEntry = value;
        await journalRepository.EditJournalAsync(SelectedDate, new JournalPatch(Date: SelectedDate, DiaryEntry: value));
    }

    public async Task ToggleNoteAsync(SleepNote note)
    {
        if (SleepNotes.Contains(note))
        {
            SleepNotes.Remove(note);
        }
        else
        {
            SleepNotes.Add(note);
        }
        await journalRepository.EditJournalAsync(SelectedDate, new JournalPatch(Date: SelectedDate, SleepNotes: SleepNotes.ToArray()));
    }
}

public sealed partial class StatisticsViewModel : ObservableObject
{
    [ObservableProperty] private bool daily = true;
    [ObservableProperty] private bool normalUi = true;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private bool snoringClipTab = true;

    public IEnumerable<DateTime> WeekDates
    {
        get
        {
            DateTime sunday = SelectedDate.Date.AddDays(-(int)SelectedDate.DayOfWeek);
            return Enumerable.Range(0, 7).Select(offset => sunday.AddDays(offset));
        }
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(WeekDates));
    }
}

public sealed partial class OnboardingViewModel(IGeneralSleepRepository generalSleepRepository, ProfileViewModel profile) : ObservableObject
{
    [ObservableProperty] private int stepIndex;
    [ObservableProperty] private string selectedSleepDuration = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public ProfileViewModel Profile => profile;

    public bool IsFirstStep => StepIndex == 0;
    public bool IsLastStep => StepIndex == 7;

    partial void OnStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsFirstStep));
        OnPropertyChanged(nameof(IsLastStep));
    }

    public void Back()
    {
        if (StepIndex > 0)
        {
            StepIndex--;
        }
    }

    public async Task ContinueAsync()
    {
        ErrorMessage = string.Empty;
        if (StepIndex == 5 && !Profile.Preferences.AgreedToPrivacyPolicy)
        {
            ErrorMessage = "Please agree to the Privacy Policy to continue.";
            return;
        }

        if (StepIndex == 6)
        {
            Profile.MarkPrivacyComplete();
        }

        if (StepIndex < 7)
        {
            StepIndex++;
            return;
        }

        if (!string.IsNullOrWhiteSpace(SelectedSleepDuration))
        {
            await generalSleepRepository.SaveSleepDataAsync(SelectedSleepDuration);
        }
        Profile.MarkAppComplete();
    }
}
