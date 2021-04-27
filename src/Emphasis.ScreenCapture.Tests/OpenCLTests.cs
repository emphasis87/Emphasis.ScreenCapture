using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Emphasis.OpenCL.Bitmap;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Silk.NET.OpenCL;
using static Emphasis.ScreenCapture.Tests.TestHelper;

namespace Emphasis.ScreenCapture.Tests
{
	[NonParallelizable]
	public class OpenCLTests
	{
		private static unsafe string GetPlatformName(CL api, nint platformId)
		{
			nuint nameSize;
			var namePtr = stackalloc byte[256];
			var err = api.GetPlatformInfo(platformId, (uint)CLEnum.PlatformName, Size<byte>(256), namePtr, &nameSize);
			if (err != 0)
				throw new Exception("Unable to get platform info: CL_PLATFORM_NAME.");

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

			var (platformId, deviceId) = FindFirstGpuPlatform(api);
			if (platformId == default)
				throw new Exception("No GPU device found.");

			var platformName = GetPlatformName(api, platformId);
			Console.WriteLine($"Platform: {platformName}");

			var contextId = CreateContext(api, platformId, deviceId);

			var queueId = api.CreateCommandQueue(contextId, deviceId, default, out var err);
			if (err != 0)
				throw new Exception("Unable to create command queue.");

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

			var (platformId, deviceId) = FindGpuPlatform(api, preferIntegrated: true);
			if (platformId == default)
				throw new Exception("No GPU device available.");
				
			var platformName = GetPlatformName(api, platformId);
			Console.WriteLine($"Platform: {platformName}");
				
			var contextId = CreateContextWithD3D11Sharing(api, platformId, deviceId, dxgiCapture);

			var queueId = api.CreateCommandQueue(contextId, deviceId, default, out var err);
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

				// Add delay to prevent stalling
				await Task.Delay(50);
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
