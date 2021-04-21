using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiScreenCaptureModule : IScreenCaptureModule
	{
		public void Configure(IServiceCollection services)
		{
			var method = new DxgiScreenCaptureMethod();
			services.AddSingleton<IScreenProvider>(method);
			services.AddSingleton<IScreenCaptureMethod>(method);

			var exporter = new DxgiScreenCaptureExporter();
			services.AddSingleton<IScreenCaptureBitmapFactory>(exporter);
		}
	}
}
