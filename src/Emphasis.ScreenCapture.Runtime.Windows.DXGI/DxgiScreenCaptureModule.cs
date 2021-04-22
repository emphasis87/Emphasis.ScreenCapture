using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiScreenCaptureModule : ScreenCaptureModule
	{
		private static readonly DxgiScreenCaptureModule Instance = new DxgiScreenCaptureModule();
		
		public override int Priority => 0;
		
		protected override IServiceCollection CreateServices()
		{
			var screens = new DxgiScreenProvider();
			var capture = new DxgiScreenCaptureMethod(this);
			var exporter = new DxgiScreenCaptureExporter();

			var services = new ServiceCollection();
			services.AddSingleton<IScreenProvider>(screens);
			services.AddSingleton<IScreenCaptureMethod>(capture);

			services.AddSingleton<IDxgiScreenProvider>(screens);
			services.AddSingleton<IDxgiScreenCaptureMethod>(capture);
			services.AddSingleton<IDxgiScreenCaptureExporter>(exporter);

			return services;
		}
	}
}
