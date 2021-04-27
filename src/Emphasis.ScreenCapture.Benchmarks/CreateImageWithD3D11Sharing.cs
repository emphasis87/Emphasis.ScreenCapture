using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI;
using FluentAssertions;
using Silk.NET.OpenCL;
using Silk.NET.OpenCL.Extensions.KHR;
using static Emphasis.ScreenCapture.Benchmarks.BenchmarkHelper;

namespace Emphasis.ScreenCapture.Benchmarks
{
	public class CreateImageWithD3D11Sharing
	{
		private IAsyncEnumerator<IScreenCapture> _captureStream;
		private nint _contextId;
		private nint _platformId;
		private nint _deviceId;
		private nint _queueId;
		private ScreenCaptureManager _manager;
		private IScreen _screen;
		private CL _api;

		[GlobalSetup]
		public async Task Setup()
		{
			_manager = new ScreenCaptureManager();
			_screen = _manager.GetScreens().FirstOrDefault();
			_captureStream = _manager.CaptureStream(_screen).GetAsyncEnumerator();
			_api = CL.GetApi();

			if (!await _captureStream.MoveNextAsync())
				throw new Exception("Unable to capture screen.");

			using var capture = _captureStream.Current;
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

		[Benchmark]
		public async Task CreateImage_with_KHR_D3D11_sharing()
		{
			if (!await _captureStream.MoveNextAsync())
				throw new Exception("Unable to capture screen.");

			using var capture = _captureStream.Current;

			var image = await capture.CreateImage(_contextId, _queueId);
			if (image.IsAcquiringRequired)
			{
				image.AcquireObject(null, out _);
				image.ReleaseObject(null, out _);
			}

			_api.Finish(_queueId);
			_api.ReleaseMemObject(image.ImageId);
		}

		private static unsafe void NotifyErr(byte* errinfo, void* privateinfo, nuint cb, void* userdata)
		{
			Console.WriteLine($"Error: {Marshal.PtrToStringAnsi((nint)errinfo)}");
		}
	}
}
