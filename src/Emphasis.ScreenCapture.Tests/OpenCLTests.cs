using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Emphasis.OpenCL.Bitmap;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Silk.NET.OpenCL;
using Silk.NET.OpenCL.Extensions.KHR;
using static Emphasis.ScreenCapture.Tests.TestHelper;

namespace Emphasis.ScreenCapture.Tests
{
	[NonParallelizable]
	public class OpenCLTests
	{
		private static nuint Size<T>(int count) => (nuint)(Marshal.SizeOf<T>() * count);

		private unsafe string GetString(byte* src, int size)
		{
			var srcSpan = new Span<byte>(src, size);
			var str = Encoding.ASCII.GetString(srcSpan.ToArray(), 0, size);
			return str;
		}

		private unsafe string GetPlatformName(CL api, nint platformId)
		{
			nuint nameSize;
			var namePtr = stackalloc byte[256];
			var err = api.GetPlatformInfo(platformId, (uint)CLEnum.PlatformName, Size<byte>(256), namePtr, &nameSize);
			err.Should().Be(0);
			var name = GetString(namePtr, (int)nameSize - 1);
			return name;
		}

		[TestCase(false)]
		[TestCase(true)]
		public async Task CreateImage_CopyHostPtr(bool reuseImage)
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

				var platformName = GetPlatformName(api, platformId);
				Console.WriteLine($"Platform: {platformName}");

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
			
			var bitmap = api.CreateBitmap(queueId, image.ImageId);

			api.ReleaseMemObject(image.ImageId);
			capture.Dispose();

			Run(bitmap, "screen.png");

			bitmap.Dispose();

			// Benchmark
			await CreateImage_benchmark(captureStream, api, contextId, queueId, reuseImage);

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
				uint numPlatforms;
				var platformIds = stackalloc nint[32];
				err = api.GetPlatformIDs(32, platformIds, &numPlatforms);
				err.Should().Be(0);

				platformId = platformIds[1];
				var platformName = GetPlatformName(api, platformId);
				Console.WriteLine($"Platform: {platformName}");

				err = api.GetDeviceIDs(platformId, CLEnum.DeviceTypeGpu, 1, &deviceId, null);
				err.Should().Be(0);

				nuint size;
				var extPtr = stackalloc byte[2048];
				err = api.GetDeviceInfo(deviceId, (uint)CLEnum.DeviceExtensions, 2048, extPtr, &size);
				err.Should().Be(0);

				var extSize = (int) size;
				bool CheckExtension(string checkedExtension)
				{
					var ext = new Span<byte>(extPtr, 2048);
					var extensions = Encoding.ASCII.GetString(ext.ToArray(), 0, extSize);
					return extensions.Contains(checkedExtension);
				}

				nint* props = default;
				if (CheckExtension("cl_khr_d3d11_sharing"))
				{
					var p = stackalloc nint[]
					{
						(nint)CLEnum.ContextPlatform, platformId,
						(nint)KHR.ContextD3D11DeviceKhr, dxgiCapture.Device.NativePointer,
						(nint)CLEnum.ContextInteropUserSync, (nint)CLEnum.False,
						0
					};
					props = p;
				}
				else if (CheckExtension("cl_nv_d3d11_sharing"))
				{
					var p = stackalloc nint[]
					{
						(nint)CLEnum.ContextPlatform, platformId,
						(nint)NV.CL_CONTEXT_D3D11_DEVICE_NV, dxgiCapture.Device.NativePointer,
						(nint)CLEnum.ContextInteropUserSync, (nint)CLEnum.False,
						0
					};
					props = p;
				}

				if (props == default)
					throw new AssertionException("D3D11 sharing is not supported.");
				
				contextId = api.CreateContext(props, 1, &deviceId, Notify, null, &err);
				err.Should().Be(0);
			}

			var queueId = api.CreateCommandQueue(contextId, deviceId, default, out err);
			err.Should().Be(0);
			
			var image = await capture.CreateImage(contextId, queueId);
			var imageId = image.ImageId;

			image.IsAcquiringRequired.Should().BeTrue();
			image.AcquireObject(null, out _);

			var bitmap = api.CreateBitmap(queueId, imageId);

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

		private static async Task CreateImage_benchmark(IAsyncEnumerator<IScreenCapture> captureStream, CL api, nint contextId, nint queueId, bool reuseImage = false)
		{
			nint imageId = default;
			var sw = new Stopwatch();
			var n = 100;
			for (var i = 0; i < n; i++)
			{
				if (!await captureStream.MoveNextAsync())
					throw new AssertionException("Unable to capture screen.");

				var capture = captureStream.Current;
				
				sw.Start();
				var image = await capture.CreateImage(contextId, queueId, reuseImage ? imageId : default);
				if (image.IsAcquiringRequired)
				{
					image.AcquireObject(null, out _);
					image.ReleaseObject(null, out _);
				}

				api.Finish(queueId);

				imageId = image.ImageId;
				if (!reuseImage)
					api.ReleaseMemObject(imageId);

				capture.Dispose();
				sw.Stop();
			}

			if (reuseImage && imageId != default)
				api.ReleaseMemObject(imageId);
			
			Console.WriteLine($"Average: {(int)(sw.Elapsed.TotalMicroseconds() / n)} us");
		}

		private static unsafe void Notify(byte* errinfo, void* privateinfo, nuint cb, void* userdata)
		{
			Console.WriteLine($"Error: {Marshal.PtrToStringAnsi((nint)errinfo)}");
		}
	}
}
