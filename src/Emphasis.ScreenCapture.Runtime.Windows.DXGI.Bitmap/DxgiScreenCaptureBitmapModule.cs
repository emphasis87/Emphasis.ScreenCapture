using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI.Bitmap
{
	public class DxgiScreenCaptureBitmapModule : ScreenCaptureModule
	{
		public override int Priority => 1;

		protected override IServiceCollection CreateServices()
		{
			var bitmapFactory = new DxgiScreenCaptureBitmapFactory();

			var services = new ServiceCollection();
			services.AddSingleton<IScreenCaptureBitmapFactory>(bitmapFactory);

			services.AddSingleton<IDxgiScreenCaptureBitmapFactory>(bitmapFactory);

			return services;
		}
	}
}
