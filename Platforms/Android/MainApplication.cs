using Android.App;
using Android.Runtime;

namespace SleepTrackerMaui;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	public override void OnCreate()
	{
		// MIGRATION: Native Android startup failures can happen before MAUI has
		//            rendered a page, so log them explicitly to keep device-only
		//            crashes diagnosable during the migration.
		AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
		{
			Android.Util.Log.Error("SleepTrackerMaui", args.Exception.ToString());
		};
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			Android.Util.Log.Error("SleepTrackerMaui", args.ExceptionObject?.ToString() ?? "Unknown unhandled exception");
		};
		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			Android.Util.Log.Error("SleepTrackerMaui", args.Exception.ToString());
		};

		base.OnCreate();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
