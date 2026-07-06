namespace SleepTrackerMaui.Services;

public interface IForegroundAccelerometerController
{
    void Start();
    void Stop();
}

public sealed partial class ForegroundAccelerometerController : IForegroundAccelerometerController
{
    public partial void Start();
    public partial void Stop();
}
