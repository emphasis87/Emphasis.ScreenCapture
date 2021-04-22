using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiScreenCaptureModule : IScreenCaptureModule
	{
		private static readonly DxgiScreenCaptureModule Instance = new DxgiScreenCaptureModule();

		private IServiceCollection _services;

		private IServiceCollection CreateServices()
		{
			var screens = new DxgiScreenProvider();
			var capture = new DxgiScreenCaptureMethod(this);
			var exporter = new DxgiScreenCaptureExporter();

			var services = new ServiceCollection();
			services.AddSingleton<IScreenProvider>(screens);
			services.AddSingleton<IScreenCaptureMethod>(capture);
			services.AddSingleton<IScreenCaptureBitmapFactory>(exporter);

			return services;
		}

		public void Configure(IServiceCollection services)
		{
			IServiceCollection local;
			lock (Instance)
			{
				local = Instance._services ??=
					Instance._services = CreateServices();
			}

			services.Add(local);
			ServiceProvider = local.BuildServiceProvider();
		}

		public IServiceProvider ServiceProvider { get; private set; }
		public int Priority => 0;
	}
}
