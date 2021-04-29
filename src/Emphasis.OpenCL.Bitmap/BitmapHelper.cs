using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Silk.NET.OpenCL;

namespace Emphasis.OpenCL.Bitmap
{
	public static class BitmapHelper
	{
		private static nuint Size<T>(int count) => (nuint)(Marshal.SizeOf<T>() * count);

		private static readonly Lazy<CL> OpenCLApiLazy = new(CL.GetApi);

		public static unsafe System.Drawing.Bitmap CreateBitmap(nint queueId, nint imageId)
		{
			var api = OpenCLApiLazy.Value;
			var err = api.GetImageInfo(imageId, (uint)CLEnum.ImageWidth, Size<nuint>(1), out nuint width, out _);
			if (err != 0)
				throw new Exception($"Unable to get image width. OpenCL error: {err}.");

			err = api.GetImageInfo(imageId, (uint)CLEnum.ImageHeight, Size<nuint>(1), out nuint height, out _);
			if (err != 0)
				throw new Exception($"Unable to get image height. OpenCL error: {err}.");

			err = api.GetImageInfo(imageId, (uint)CLEnum.ImageElementSize, Size<nuint>(1), out nuint elementSize, out _);
			if (err != 0)
				throw new Exception($"Unable to get image element size. OpenCL error: {err}.");

			err = api.GetImageInfo(imageId, (uint)CLEnum.ImageRowPitch, Size<nuint>(1), out nuint rowPitch, out _);
			if (err != 0)
				throw new Exception($"Unable to get image row pitch. OpenCL error: {err}.");

			var imageFormat = stackalloc uint[2];
			err = api.GetImageInfo(imageId, (uint)CLEnum.ImageFormat, Size<uint>(2), imageFormat, out _);
			if (err != 0)
				throw new Exception($"Unable to get image format. OpenCL error: {err}.");

			var w = (int)width;
			var h = (int)height;
			var bpp = (int)elementSize;

			var channelOrder = imageFormat[0];
			var channelType = imageFormat[1];

			var pixelFormat = default(PixelFormat);
			if (channelOrder == (int)CLEnum.Bgra && (channelType == (int)CLEnum.UnsignedInt8 || channelType == (int)CLEnum.UnormInt8))
			{
				pixelFormat = PixelFormat.Format32bppArgb;
			}

			var result = new byte[w * h * bpp];

			var origin = stackalloc nuint[3] { 0, 0, 0 };
			var region = stackalloc nuint[3] { width, height, 1 };
			var resultPtr = GCHandle.Alloc(result, GCHandleType.Pinned);
			try
			{
				nint evtReadImage;
				err = api.EnqueueReadImage(queueId, imageId, true, origin, region, 0, 0, (void*)resultPtr.AddrOfPinnedObject(), 0, null, &evtReadImage);
				if (err != 0)
					throw new Exception($"Unable to enqueue read image. OpenCL error: {err}.");

				var sourcePtr = resultPtr.AddrOfPinnedObject();
				var bitmap = new System.Drawing.Bitmap(w, h, pixelFormat);
				var boundsRect = new System.Drawing.Rectangle(0, 0, w, h);

				// Copy pixels from screen capture Texture to GDI bitmap
				var mapTarget = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

				var targetPtr = mapTarget.Scan0;
				for (var y = 0; y < h; y++)
				{
					// Copy a single line
					var target = new Span<byte>((void*) targetPtr, w * bpp);
					var source = new Span<byte>((void*) sourcePtr, w * bpp);

					source.CopyTo(target);

					// Advance pointers
					sourcePtr = IntPtr.Add(sourcePtr, (int)rowPitch);
					targetPtr = IntPtr.Add(targetPtr, mapTarget.Stride);
				}

				// Release source and dest locks
				bitmap.UnlockBits(mapTarget);

				return bitmap;
			}
			finally
			{
				resultPtr.Free();
			}
		}
	}
}
