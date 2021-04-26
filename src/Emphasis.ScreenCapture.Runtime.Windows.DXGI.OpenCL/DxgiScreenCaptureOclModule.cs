using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI.OpenCL
{
	public class DxgiScreenCaptureOclModule : ScreenCaptureModule
	{
		public override int Priority => 1;

		protected override IServiceCollection CreateServices()
		{
			var bitmapFactory = new DxgScreenCaptureOclImageFactory();

			var services = new ServiceCollection();
			services.AddSingleton<IScreenCaptureOclImageFactory>(bitmapFactory);

			services.AddSingleton<IDxgiScreenCaptureOclImageFactory>(bitmapFactory);

			return services;
		}
	}
}
