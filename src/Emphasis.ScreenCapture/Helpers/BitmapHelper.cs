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

		public static Bitmap ToBitmap(this byte[] data, int width, int height, PixelFormat format)
		{
			var targetHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var targetPointer = targetHandle.AddrOfPinnedObject();

			var result = new Bitmap(width, height);

			var bounds = new System.Drawing.Rectangle(0, 0, width, height);
			var resultData = result.LockBits(bounds, ImageLockMode.WriteOnly, format);
			var resultPointer = resultData.Scan0;

			for (var y = 0; y < height; y++)
			{
				Utilities.CopyMemory(resultPointer, targetPointer, width * 4);

				targetPointer = IntPtr.Add(targetPointer, width * 4);
				resultPointer = IntPtr.Add(resultPointer, width * 4);
			}

			result.UnlockBits(resultData);

			targetHandle.Free();

			return result;
		}
	}
}
