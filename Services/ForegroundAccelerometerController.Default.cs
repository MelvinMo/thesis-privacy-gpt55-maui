#if !ANDROID
namespace SleepTrackerMaui.Services;

public sealed partial class ForegroundAccelerometerController
{
    public partial void Start()
    {
        // MIGRATION_FLAG: Non-Android targets do not support Android
        //                 ForegroundService notifications. Shared sensor
        //                 monitoring stays foreground-only on those targets.
    }

    public partial void Stop()
    {
        // MIGRATION: iOS/macOS/Windows do not have the Android foreground
        //            notification contract, so stopping is a no-op here.
    }
}
#endif
