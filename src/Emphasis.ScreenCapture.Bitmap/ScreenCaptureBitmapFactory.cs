using System;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureBitmapFactory
	{
		Task<Bitmap> ToBitmap(IScreenCapture capture);
	}

	public static class ScreenCaptureBitmapExtensions
	{
		public static Task<Bitmap> ToBitmap(this IScreenCapture capture)
		{
			var module = capture.Module;
			var services = module.ServiceProvider 
				?? throw new ArgumentNullException(nameof(IScreenCaptureModule.ServiceProvider), "ServiceProvider is null.");
			var factory = services.GetService<IScreenCaptureBitmapFactory>()
				?? throw new NotSupportedException($"{module.GetType()} does not support {typeof(IScreenCaptureBitmapFactory)}.");
			return factory.ToBitmap(capture);
		}
	}
}
