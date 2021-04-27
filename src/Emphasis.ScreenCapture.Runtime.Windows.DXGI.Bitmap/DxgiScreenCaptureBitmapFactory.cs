using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SharpDX;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI.Bitmap
{
	public interface IDxgiScreenCaptureBitmapFactory : IScreenCaptureBitmapFactory
	{

	}

	public class DxgiScreenCaptureBitmapFactory : IDxgiScreenCaptureBitmapFactory
	{
		public async Task<System.Drawing.Bitmap> ToBitmap(IScreenCapture capture)
		{
			if (capture is not DxgiScreenCapture dxgiCapture)
				throw new ArgumentOutOfRangeException(nameof(capture), $"Only {typeof(DxgiScreenCapture)} is supported.");

			var width = dxgiCapture.Width;
			var height = dxgiCapture.Height;

			var exporter = dxgiCapture.ServiceProvider.GetRequiredService<IDxgiScreenCaptureExporter>();

			using var texture = await exporter.MapTexture(dxgiCapture);

			// Create Drawing.Bitmap
			var bitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
			var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

			// Copy pixels from screen capture Texture to GDI bitmap
			var mapTarget = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
			var sourcePtr = texture.DataPointer;
			var targetPtr = mapTarget.Scan0;
			for (var y = 0; y < height; y++)
			{
				// Copy a single line 
				Utilities.CopyMemory(targetPtr, sourcePtr, width * 4);

				// Advance pointers
				sourcePtr = IntPtr.Add(sourcePtr, texture.RowPitch);
				targetPtr = IntPtr.Add(targetPtr, mapTarget.Stride);
			}

			// Release source and dest locks
			bitmap.UnlockBits(mapTarget);

			return bitmap;
		}
	}
}
