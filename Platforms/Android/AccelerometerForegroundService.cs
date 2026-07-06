#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace SleepTrackerMaui;

[Service(Name = "com.mcscert.sleeptracker.mauidev.AccelerometerForegroundService", Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
public sealed class AccelerometerForegroundService : Service
{
    private const string ChannelId = "sleep_tracker_accelerometer";
    private const int NotificationId = 53079;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        NotificationCompat.Builder builder = new(this, ChannelId);
        builder.SetContentTitle("GPT Sleep Tracker MAUI");
        builder.SetContentText("Monitoring sleep movement");
        builder.SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo);
        builder.SetOngoing(true);
        Notification? notification = builder.Build();
        StartForeground(NotificationId, notification ?? throw new InvalidOperationException("Foreground notification could not be created."));
        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        NotificationChannel channel = new(ChannelId, "Sleep movement monitoring", NotificationImportance.Low)
        {
            Description = "Persistent notification for background accelerometer monitoring"
        };
        NotificationManager? manager = GetSystemService(NotificationService) as NotificationManager;
        manager?.CreateNotificationChannel(channel);
    }
}
#endif
