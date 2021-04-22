using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiScreenCaptureModule : IScreenCaptureModule
	{
		public void Configure(IServiceCollection services)
		{
			var screens = new DxgiScreenProvider();
			var capture = new DxgiScreenCaptureMethod(this);
			var exporter = new DxgiScreenCaptureExporter();
			
			var local = new ServiceCollection();
			local.AddSingleton<IScreenProvider>(screens);
			local.AddSingleton<IScreenCaptureMethod>(capture);
			local.AddSingleton<IScreenCaptureBitmapFactory>(exporter);

			services.Add(local);

			ServiceProvider = local.BuildServiceProvider();
		}

		public IServiceProvider ServiceProvider { get; private set; }
		public int Priority => 0;
	}
}
