using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;

namespace Emphasis.ScreenCapture.Helpers
{
	public static class BitmapHelpers
	{
		public static byte[] ToBytes(this Bitmap bitmap)
		{
			var w = bitmap.Width;
			var h = bitmap.Height;
			var bounds = new System.Drawing.Rectangle(0, 0, w, h);
			var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			var result = new byte[h * w * 4];
			var resultHandler = GCHandle.Alloc(result, GCHandleType.Pinned);
			var resultPointer = resultHandler.AddrOfPinnedObject();

			var sourcePointer = data.Scan0;
			for (var y = 0; y < h; y++)
			{
				Utilities.CopyMemory(resultPointer, sourcePointer, w * 4);

				sourcePointer = IntPtr.Add(sourcePointer, data.Stride);
				resultPointer = IntPtr.Add(resultPointer, w * 4);
			}

			bitmap.UnlockBits(data);

			resultHandler.Free();

			return result;
		}

		public static Bitmap ToBitmap(this byte[] data, int width, int height, int channels = 1)
		{
			var bitmap = new Bitmap(width, height);
			var bounds = new System.Drawing.Rectangle(0, 0, width, height);

			var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			var bitmapPointer = bitmapData.Scan0;

			// Grayscale
			if (channels == 1)
			{
				var p = 0;
				var source = new byte[height * width * 4];
				for (var i = 0; i < height * width; i++)
				{
					var value = data[i];
					source[p++] = value;
					source[p++] = value;
					source[p++] = value;
					source[p++] = 255;
				}

				data = source;
			}

			var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var dataPointer = dataHandle.AddrOfPinnedObject();

			for (var y = 0; y < height; y++)
			{
				Utilities.CopyMemory(bitmapPointer, dataPointer, width * 4);

				dataPointer = IntPtr.Add(dataPointer, width * 4);
				bitmapPointer = IntPtr.Add(bitmapPointer, width * 4);
			}

			bitmap.UnlockBits(bitmapData);

			dataHandle.Free();

			return bitmap;
		}
	}
}
