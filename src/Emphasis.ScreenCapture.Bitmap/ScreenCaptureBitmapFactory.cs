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
			var serviceProvider = capture.ServiceProvider
				?? throw new ArgumentNullException(nameof(capture.ServiceProvider), $"{nameof(capture.ServiceProvider)} is null.");
			var factory = serviceProvider.GetService<IScreenCaptureBitmapFactory>()
				?? throw new NotSupportedException($"{capture.GetType()} does not support {typeof(IScreenCaptureBitmapFactory)}.");
			return factory.ToBitmap(capture);
		}
	}
}
