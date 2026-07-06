namespace SleepTrackerMaui;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		InitializeComponent();
		// MIGRATION: AppShell is resolved after Application resources are
		//            initialized because MAUI Shell needs an active application
		//            lifecycle before it can safely attach to a Window.
		_services = services;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_services.GetRequiredService<AppShell>());
	}
}
