using Microsoft.Extensions.Hosting;
using Microsoft.Maui.Controls.Shapes;
using SleepTrackerMaui.Repositories;
using SleepTrackerMaui.Stores;

namespace SleepTrackerMaui.Services;

public interface IDeviceSensorService
{
    bool IsAccelerometerAvailable { get; }
    bool IsLightSensorAvailable { get; }
    Task StartAccelerometerAsync();
    Task StopAccelerometerAsync();
}

public sealed class DeviceSensorService(ISensorRepository sensorRepository, TransparencyStore transparencyStore) : IDeviceSensorService
{
    public bool IsAccelerometerAvailable => Accelerometer.Default.IsSupported;

    public bool IsLightSensorAvailable =>
#if IOS
        false;
#elif ANDROID
        // MIGRATION: Expo LightSensor maps to Android SensorManager. iOS keeps
        //            the required graceful stub, but Android should report the
        //            real hardware capability instead of disabling the feature.
        Platform.AppContext.GetSystemService(Android.Content.Context.SensorService)
            is Android.Hardware.SensorManager manager
            && manager.GetDefaultSensor(Android.Hardware.SensorType.Light) is not null;
#else
        false;
#endif

    public Task StartAccelerometerAsync()
    {
        if (!Accelerometer.Default.IsSupported || Accelerometer.Default.IsMonitoring)
        {
            return Task.CompletedTask;
        }

        Accelerometer.Default.ReadingChanged += OnReadingChanged;
        Accelerometer.Default.Start(SensorSpeed.UI);
        return transparencyStore.SetAccelerometerAsync(transparencyStore.Accelerometer with
        {
            SensorType = "accelerometer",
            SamplingRate = 1,
            BackgroundMode = true
        });
    }

    public Task StopAccelerometerAsync()
    {
        if (Accelerometer.Default.IsMonitoring)
        {
            Accelerometer.Default.Stop();
            Accelerometer.Default.ReadingChanged -= OnReadingChanged;
        }
        return Task.CompletedTask;
    }

    private async void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        try
        {
            await sensorRepository.SaveAccelerometerSampleAsync(e.Reading.Acceleration.X, e.Reading.Acceleration.Y, e.Reading.Acceleration.Z);
        }
        catch
        {
            // MIGRATION_FLAG: Sensor persistence failures should not crash the
            //                 foreground UI while hardware readings continue.
        }
    }
}

public sealed class AccelerometerBackgroundWorker(IDeviceSensorService sensors) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // MIGRATION: Expo background tasks become a .NET BackgroundService
        //            abstraction in shared code. Android promotes this work to
        //            a platform ForegroundService with a persistent notification.
        await sensors.StartAccelerometerAsync();
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        await sensors.StopAccelerometerAsync();
    }
}

public sealed class SensorNotAvailableWidget : ContentView
{
    public SensorNotAvailableWidget()
    {
        Content = new Border
        {
            BackgroundColor = AppColors.LightBlack,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = 16,
            Content = new Label
            {
                Text = "This sensor is not available on this platform.",
                TextColor = Colors.White,
                FontFamily = "SpaceMono",
                FontSize = 14
            }
        };
    }
}
