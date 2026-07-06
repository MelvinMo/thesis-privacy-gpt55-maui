#if ANDROID
using Android.Content;

namespace SleepTrackerMaui.Services;

public sealed partial class ForegroundAccelerometerController
{
    public partial void Start()
    {
        // MIGRATION: Expo background tasks become an explicit Android
        //            ForegroundService on Android 8+ so the persistent
        //            notification requirement is satisfied by the platform.
        Intent intent = new(Platform.AppContext, typeof(AccelerometerForegroundService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            Platform.AppContext.StartForegroundService(intent);
        }
        else
        {
            Platform.AppContext.StartService(intent);
        }
    }

    public partial void Stop()
    {
        Intent intent = new(Platform.AppContext, typeof(AccelerometerForegroundService));
        Platform.AppContext.StopService(intent);
    }
}
#endif
