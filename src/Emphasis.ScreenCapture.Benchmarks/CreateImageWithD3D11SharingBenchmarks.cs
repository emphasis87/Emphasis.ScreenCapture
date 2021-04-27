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
using Silk.NET.OpenCL.Extensions.KHR;
using static Emphasis.ScreenCapture.Benchmarks.BenchmarkHelper;

namespace Emphasis.ScreenCapture.Benchmarks
{
	[SimpleJob(id: "burst", invocationCount: 100, warmupCount: 0)]
	[SimpleJob(id: "heavy load", invocationCount: 200, warmupCount: 20)]
	public class CreateImageWithD3D11SharingBenchmarks
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

				nuint size;
				var extPtr = stackalloc byte[2048];
				err = _api.GetDeviceInfo(_deviceId, (uint)CLEnum.DeviceExtensions, 2048, extPtr, &size);
				err.Should().Be(0);
				var extensions = GetString(extPtr, (int) size);

				nint* props = default;
				if (extensions.Contains("cl_khr_d3d11_sharing"))
				{
					var p = stackalloc nint[]
					{
						(nint)CLEnum.ContextPlatform, _platformId,
						(nint)KHR.ContextD3D11DeviceKhr, dxgiCapture.Device.NativePointer,
						(nint)CLEnum.ContextInteropUserSync, (nint)CLEnum.False,
						0
					};
					props = p;
				}
				else if (extensions.Contains("cl_nv_d3d11_sharing"))
				{
					var p = stackalloc nint[]
					{
						(nint)CLEnum.ContextPlatform, _platformId,
						(nint)NV.CL_CONTEXT_D3D11_DEVICE_NV, dxgiCapture.Device.NativePointer,
						(nint)CLEnum.ContextInteropUserSync, (nint)CLEnum.False,
						0
					};
					props = p;
				}

				if (props == default)
					throw new Exception("D3D11 sharing is not supported.");

				_contextId = _api.CreateContext(props, 1, &deviceId, NotifyErr, null, &err);
				err.Should().Be(0);
			}

			var queueId = _api.CreateCommandQueue(_contextId, _deviceId, default, out err);
			err.Should().Be(0);
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

		private static unsafe void NotifyErr(byte* errinfo, void* privateinfo, nuint cb, void* userdata)
		{
			Console.WriteLine($"Error: {Marshal.PtrToStringAnsi((nint)errinfo)}");
		}
	}
}
