using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI.Bitmap;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI.OpenCL;
using Silk.NET.OpenCL;
using static Emphasis.ScreenCapture.Tests.TestHelper;

namespace Emphasis.ScreenCapture.Benchmarks
{
	[MarkdownExporter]
	[SimpleJob(id: "burst", invocationCount: 1000, warmupCount: 0)]
	[SimpleJob(id: "heavy load", invocationCount: 1000, warmupCount: 20)]
	[BenchmarkCategory("integrated-gpu")]
	public class CreateImageWithD3D11SharingIntegratedGpuBenchmarks
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

			(_platformId, _deviceId) = FindIntegratedGpuPlatform(_api);
			if (_deviceId == default)
				throw new Exception("No integrated GPU device found.");

			_contextId = CreateContextWithD3D11Sharing(_api, _platformId, _deviceId, dxgiCapture);

			var queueId = _api.CreateCommandQueue(_contextId, _deviceId, default, out var err);
			if (err != 0)
				throw new Exception("Unable to create command queue.");

			_queueId = queueId;
		}

		[GlobalCleanup]
		public void Cleanup()
		{
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
		public async Task CreateImage_with_KHR_D3D11_sharing()
		{
			var image = await _screenCapture.CreateImage(_contextId, _queueId);

			image.AcquireObject(null, out _);
			image.ReleaseObject(null, out _);

			_api.Finish(_queueId);
			_api.ReleaseMemObject(image.ImageId);
		}
	}
}