using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureBitmapFactory
	{
		Task<Bitmap> ToBitmap(IScreenCapture capture);
	}

	public static class ScreenCaptureBitmapExtensions
	{
		public static Task<Bitmap> ToBitmap(IScreenCapture capture)
		{
			var method = capture.Method;
			return method switch
			{
				null => throw new ArgumentNullException(nameof(capture.Method), "Screen capture method is null."),
				IScreenCaptureBitmapFactory factory => factory.ToBitmap(capture),
				_ => throw new NotSupportedException($"{method.GetType()} does not support {typeof(IScreenCaptureBitmapFactory)}.")
			};
		}
	}
}
