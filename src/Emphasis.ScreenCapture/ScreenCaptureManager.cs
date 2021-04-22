﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture
{
	public class ScreenCaptureManager
	{
		private readonly Lazy<IServiceProvider> _serviceProvider =
			new Lazy<IServiceProvider>(() => new ScreenCaptureModuleLoader().ServiceProvider);
		private IServiceProvider ServiceProvider => _serviceProvider.Value;

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