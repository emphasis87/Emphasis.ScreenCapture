using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureManager
	{
		IScreen[] GetScreens();
		Task<IScreenCapture> Capture(IScreen screen, CancellationToken cancellationToken = default);
		IAsyncEnumerable<IScreenCapture> CaptureStream(IScreen screen, CancellationToken cancellationToken = default);
	}

	public class ScreenCaptureManager : IScreenCaptureManager
	{
		private readonly IScreenCaptureModuleManager _moduleManager;
		private readonly Lazy<IServiceProvider> _serviceProviderLazy;
		private IServiceProvider ServiceProvider => _serviceProviderLazy.Value;
		
		public ScreenCaptureManager(IScreenCaptureModuleManager moduleManager = null)
		{
			_moduleManager = moduleManager ?? new ScreenCaptureModuleManager();
			_serviceProviderLazy = new Lazy<IServiceProvider>(() => _moduleManager.ServiceProvider);
		}

		public IScreen[] GetScreens()
		{
			var screens = ServiceProvider.GetRequiredService<IScreenProvider>();
			return screens.GetScreens();
		}

		public Task<IScreenCapture> Capture(IScreen screen, CancellationToken cancellationToken = default)
		{
			var method = ServiceProvider.GetRequiredService<IScreenCaptureMethod>();
			return method.Capture(screen, cancellationToken);
		}

		public IAsyncEnumerable<IScreenCapture> CaptureStream(IScreen screen, CancellationToken cancellationToken = default)
		{
			var method = ServiceProvider.GetRequiredService<IScreenCaptureMethod>();
			return method.CaptureStream(screen, cancellationToken);
		}
	}
}