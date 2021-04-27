using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI.Bitmap;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI.OpenCL;
using FluentAssertions;
using Silk.NET.OpenCL;
using static Emphasis.ScreenCapture.Benchmarks.BenchmarkHelper;

namespace Emphasis.ScreenCapture.Benchmarks
{
	[SimpleJob(id: "burst", invocationCount: 100, warmupCount: 0)]
	[SimpleJob(id: "heavy load", invocationCount: 100, warmupCount: 50)]
	public class CreateImageBenchmarks
	{
		private nint _contextId;
		private nint _platformId;
		private nint _deviceId;
		private nint _queueId;
		private IScreenCaptureModuleManager _moduleManager;
		private IScreenCaptureManager _manager;
		private IScreen _screen;
		private IScreenCapture _screenCapture;
		private CL _api;
		private nint _imageId;

		[GlobalSetup]
		public async Task Setup()
		{
			_moduleManager = new ScreenCaptureModuleManager(new IScreenCaptureModule[]
			{
				new DxgiScreenCaptureModule(),
				new DxgiScreenCaptureBitmapModule(),
				new DxgiScreenCaptureOclModule()
			});
			_manager = new ScreenCaptureManager(_moduleManager);
			_screen = _manager.GetScreens().FirstOrDefault();
			_api = CL.GetApi();

			using var capture = await _manager.Capture(_screen);
			if (capture is not DxgiScreenCapture dxgiCapture)
				throw new Exception("Dxgi screen capture is required.");

			int err;

			unsafe
			{
				nint platformId;
				err = _api.GetPlatformIDs(1, &platformId, null);
				err.Should().Be(0);
				_platformId = platformId;

				nint deviceId;
				err = _api.GetDeviceIDs(_platformId, CLEnum.DeviceTypeGpu, 1, &deviceId, null);
				err.Should().Be(0);
				_deviceId = deviceId;

				var props = stackalloc nint[]
				{
					(nint)CLEnum.ContextPlatform, _platformId,
					0
				};
				
				_contextId = _api.CreateContext(props, 1, &deviceId, NotifyErr, null, &err);
				err.Should().Be(0);
			}

			var queueId = _api.CreateCommandQueue(_contextId, _deviceId, default, out err);
			err.Should().Be(0);
			_queueId = queueId;

			var image = await dxgiCapture.CreateImage(_contextId, _queueId);
			_imageId = image.ImageId;
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			if (_imageId != default)
				_api.ReleaseMemObject(_imageId);

			_api.ReleaseCommandQueue(_queueId);
			_api.ReleaseContext(_contextId);
		}

		[IterationSetup]
		public void IterationSetup()
		{
			_screenCapture = _manager.Capture(_screen).Result;
		}

		[IterationCleanup]
		public void IterationCleanup()
		{
			_screenCapture.Dispose();
		}

		[Benchmark]
		public async Task CreateImage()
		{
			var image = await _screenCapture.CreateImage(_contextId, _queueId);

			_api.Finish(_queueId);
			_api.ReleaseMemObject(image.ImageId);
		}

		[Benchmark]
		public async Task CreateImage_with_shared_buffer()
		{
			var image = await _screenCapture.CreateImage(_contextId, _queueId, _imageId);
			_imageId = image.ImageId;
			_api.Finish(_queueId);
		}

		private static unsafe void NotifyErr(byte* errinfo, void* privateinfo, nuint cb, void* userdata)
		{
			Console.WriteLine($"Error: {Marshal.PtrToStringAnsi((nint)errinfo)}");
		}
	}
}
