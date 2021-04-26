using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using SharpDX;
using Silk.NET.OpenCL;
using Silk.NET.OpenCL.Extensions.KHR;
using static Emphasis.ScreenCapture.Tests.TestHelper;

namespace Emphasis.ScreenCapture.Tests
{
	[NonParallelizable]
	public class OpenCLTests
	{
		private static nuint Size<T>(int count) => (nuint)(Marshal.SizeOf<T>() * count);

		[Test]
		public async Task CreateImage_CopyHostPtr()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();

			var api = CL.GetApi();

			int err;

			nint contextId;
			nint platformId;
			nint deviceId;
			unsafe
			{
				err = api.GetPlatformIDs(1, &platformId, null);
				err.Should().Be(0);

				err = api.GetDeviceIDs(platformId, CLEnum.DeviceTypeGpu, 1, &deviceId, null);
				err.Should().Be(0);

				var props = stackalloc nint[]{ (nint) CLEnum.ContextPlatform, platformId, 0 };
				contextId = api.CreateContext(props, 1, &deviceId, Notify, null, &err);
				err.Should().Be(0);
			}

			var queueId = api.CreateCommandQueue(contextId, deviceId, default, out err);
			err.Should().Be(0);

			var captureStream = manager.CaptureStream(screen).GetAsyncEnumerator();
			if (!await captureStream.MoveNextAsync())
				throw new AssertionException("Unable to capture screen.");

			var capture = captureStream.Current;
			var image = await capture.CreateImage(contextId, queueId);

			image.IsAcquiringRequired.Should().BeFalse();
			
			var bitmap = CreateBitmap(api, queueId, image.ImageId);

			api.ReleaseMemObject(image.ImageId);
			capture.Dispose();

			Run(bitmap, "screen.png");

			bitmap.Dispose();

			// Benchmark
			await CreateImage_benchmark(captureStream, api, contextId, queueId);

			api.ReleaseCommandQueue(queueId);
			api.ReleaseContext(contextId);
		}
		
		[Test]
		public async Task CreateImage_with_KHR_D3D11_sharing()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();
			
			var captureStream = manager.CaptureStream(screen).GetAsyncEnumerator();
			if (!await captureStream.MoveNextAsync())
				throw new AssertionException("Unable to capture screen.");

			var capture = captureStream.Current;
			if (capture is not DxgiScreenCapture dxgiCapture)
				throw new AssertionException("Dxgi screen capture is required.");

			var api = CL.GetApi();

			int err;

			nint contextId;
			nint platformId;
			nint deviceId;
			unsafe
			{
				err = api.GetPlatformIDs(1, &platformId, null);
				err.Should().Be(0);

				err = api.GetDeviceIDs(platformId, CLEnum.DeviceTypeGpu, 1, &deviceId, null);
				err.Should().Be(0);

				nuint size;
				var extPtr = stackalloc byte[2048];
				err = api.GetDeviceInfo(deviceId, (uint)CLEnum.DeviceExtensions, 2048, extPtr, &size);
				err.Should().Be(0);

				var extSize = (int) size;
				bool CheckExtension()
				{
					var ext = new Span<byte>(extPtr, 2048);
					var extensions = Encoding.ASCII.GetString(ext.ToArray(), 0, extSize);
					return extensions.Contains("cl_khr_d3d11_sharing");
				}

				CheckExtension().Should().BeTrue();
				
				var props = stackalloc nint[]
				{
					(nint)CLEnum.ContextPlatform, platformId,
					(nint)KHR.ContextD3D11DeviceKhr, dxgiCapture.Device.NativePointer,
					(nint)CLEnum.ContextInteropUserSync, (nint)CLEnum.False,
					0
				};
				contextId = api.CreateContext(props, 1, &deviceId, Notify, null, &err);
				err.Should().Be(0);
			}

			var queueId = api.CreateCommandQueue(contextId, deviceId, default, out err);
			err.Should().Be(0);
			
			var image = await capture.CreateImage(contextId, queueId);
			var imageId = image.ImageId;

			image.IsAcquiringRequired.Should().BeTrue();
			image.AcquireObject(null, out _);

			var bitmap = CreateBitmap(api, queueId, imageId);

			image.ReleaseObject(null, out _);
			
			api.ReleaseMemObject(imageId);
			capture.Dispose();
			
			Run(bitmap, "screen.png");

			bitmap.Dispose();

			// Benchmark
			await CreateImage_benchmark(captureStream, api, contextId, queueId);

			api.ReleaseCommandQueue(queueId);
			api.ReleaseContext(contextId);
		}

		private static async Task CreateImage_benchmark(IAsyncEnumerator<IScreenCapture> captureStream, CL api, nint contextId, nint queueId)
		{
			var sw = new Stopwatch();
			var n = 100;
			for (var i = 0; i < n; i++)
			{
				if (!await captureStream.MoveNextAsync())
					throw new AssertionException("Unable to capture screen.");

				var capture2 = captureStream.Current;
				
				sw.Start();
				var image2 = await capture2.CreateImage(contextId, queueId);
				if (image2.IsAcquiringRequired)
				{
					image2.AcquireObject(null, out _);
					image2.ReleaseObject(null, out _);
				}
				
				api.ReleaseMemObject(image2.ImageId);
				capture2.Dispose();
				sw.Stop();
			}
			
			Console.WriteLine($"Average: {(int)(sw.Elapsed.TotalMicroseconds() / n)} us");
		}

		private unsafe Bitmap CreateBitmap(CL api, nint queueId, nint imageId)
		{
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
					Utilities.CopyMemory(targetPtr, sourcePtr, w * bpp);

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

		private static unsafe void Notify(byte* errinfo, void* privateinfo, nuint cb, void* userdata)
		{
			Console.WriteLine($"Error: {Marshal.PtrToStringAnsi((nint)errinfo)}");
		}
	}
}
