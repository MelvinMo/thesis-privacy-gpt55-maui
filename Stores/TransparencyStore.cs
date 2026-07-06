using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using SleepTrackerMaui.Models;
using SleepTrackerMaui.Services;

namespace SleepTrackerMaui.Stores;

public sealed partial class TransparencyStore : ObservableObject
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    [ObservableProperty] private TransparencyEvent light = TransparencyDefaults.Light();
    [ObservableProperty] private TransparencyEvent microphone = TransparencyDefaults.Microphone();
    [ObservableProperty] private TransparencyEvent accelerometer = TransparencyDefaults.Accelerometer();
    [ObservableProperty] private TransparencyEvent journal = TransparencyDefaults.Journal();
    [ObservableProperty] private TransparencyEvent sleep = TransparencyDefaults.Sleep();
    [ObservableProperty] private TransparencyEvent statistics = TransparencyDefaults.Statistics();

    public async Task LoadAsync()
    {
        Light = LoadChannel("lightSensorTransparency", TransparencyDefaults.Light());
        Microphone = LoadChannel("microphoneTransparency", TransparencyDefaults.Microphone());
        Accelerometer = LoadChannel("accelerometerTransparency", TransparencyDefaults.Accelerometer());
        Journal = LoadChannel("journalTransparency", TransparencyDefaults.Journal());
        Sleep = LoadChannel("generalSleepTransparency", TransparencyDefaults.Sleep());
        Statistics = LoadChannel("statisticsTransparency", TransparencyDefaults.Statistics());
        await Task.CompletedTask;
    }

    public Task SetLightAsync(TransparencyEvent value) => SetChannelAsync("lightSensorTransparency", value, updated => Light = updated);
    public Task SetMicrophoneAsync(TransparencyEvent value) => SetChannelAsync("microphoneTransparency", value, updated => Microphone = updated);
    public Task SetAccelerometerAsync(TransparencyEvent value) => SetChannelAsync("accelerometerTransparency", value, updated => Accelerometer = updated);
    public Task SetJournalAsync(TransparencyEvent value) => SetChannelAsync("journalTransparency", value, updated => Journal = updated);
    public Task SetSleepAsync(TransparencyEvent value) => SetChannelAsync("generalSleepTransparency", value, updated => Sleep = updated);
    public Task SetStatisticsAsync(TransparencyEvent value) => SetChannelAsync("statisticsTransparency", value, updated => Statistics = updated);

    private async Task SetChannelAsync(string key, TransparencyEvent value, Action<TransparencyEvent> apply)
    {
        await _gate.WaitAsync();
        try
        {
            // MIGRATION: Zustand setters persisted each channel independently.
            //            SemaphoreSlim + ObservableObject makes every MAUI
            //            channel update atomic and immediately reactive without
            //            collapsing all six channels into one mutable blob.
            Preferences.Default.Set(key, JsonSerializer.Serialize(value, AppJson.Options));
            apply(value);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static TransparencyEvent LoadChannel(string key, TransparencyEvent fallback)
    {
        string raw = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return JsonSerializer.Deserialize<TransparencyEvent>(raw, AppJson.Options) ?? fallback;
    }
}
