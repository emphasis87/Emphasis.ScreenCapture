using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Emphasis.OpenCL.Bitmap;
using Emphasis.ScreenCapture.Runtime.Windows.DXGI;
using Silk.NET.OpenCL;
using Silk.NET.OpenCL.Extensions.KHR;

namespace Emphasis.ScreenCapture.Tests
{
	public static class TestHelper
	{
		public static void Run(Bitmap bitmap, string name)
		{
			if (!Path.HasExtension(name))
				name = $"{name}.png";

			bitmap.Save(name);
			Run(name);
		}

		public static void Run(string path, string arguments = null)
		{
			if (!Path.IsPathRooted(path) && Path.HasExtension(path))
				path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

			var info = new ProcessStartInfo(path)
			{
				UseShellExecute = true
			};
			if (arguments != null)
			{
				info.Arguments = arguments;
			}

			Process.Start(info);
		}

		public static nuint Size<T>(int count) => (nuint)(Marshal.SizeOf<T>() * count);

		public static unsafe string GetString(byte* src, int size)
		{
			var srcSpan = new Span<byte>(src, size);
			var str = Encoding.ASCII.GetString(srcSpan.ToArray(), 0, size);
			return str;
		}

		public static (nint platformId, nint deviceId) FindGpuPlatform(CL api, bool preferIntegrated = false)
		{
			nint platformId = default;
			nint deviceId = default;
			if (preferIntegrated)
				(platformId, deviceId) = FindIntegratedGpuPlatform(api);

			if (platformId == default)
				(platformId, deviceId) = FindFirstGpuPlatform(api);

			return (platformId, deviceId);
		}

		public static (nint platformId, nint deviceId) FindIntegratedGpuPlatform(CL api)
		{
			return FindGpuPlatform(api, deviceCount => deviceCount > 1);
		}

		public static (nint platformId, nint deviceId) FindDedicatedGpuPlatform(CL api)
		{
			return FindGpuPlatform(api, deviceCount => deviceCount == 1);
		}

		public static unsafe (nint platformId, nint deviceId) FindGpuPlatform(CL api, Func<int, bool> whereDeviceCount)
		{
			uint platformCount;
			var platformIds = stackalloc nint[32];
			var err = api.GetPlatformIDs(32, platformIds, &platformCount);
			if (err != 0)
				throw new Exception("Unable to get platform ids.");

			if (platformCount == 0)
				throw new Exception("No OpenCL platforms available.");

			var deviceIds = stackalloc nint[32];
			for (var pi = 0; pi < platformCount; pi++)
			{
				uint deviceCount;
				err = api.GetDeviceIDs(platformIds[pi], CLEnum.DeviceTypeAll, 32, deviceIds, &deviceCount);
				if (err != 0)
					throw new Exception("Unable to get device ids.");

				if (!whereDeviceCount((int)deviceCount))
					continue;

				for (var di = 0; di < deviceCount; di++)
				{
					nuint deviceType;
					err = api.GetDeviceInfo(deviceIds[di], (uint)CLEnum.DeviceType, Size<nuint>(1), &deviceType, out var size);
					if (err != 0)
						throw new Exception("Unable to get device info: CL_DEVICE_TYPE.");

					if (deviceType != (uint)CLEnum.DeviceTypeGpu)
						continue;

					return (platformIds[pi], deviceIds[di]);
				}
			}

			return default;
		}

		public static unsafe (nint platformId, nint deviceId) FindFirstGpuPlatform(CL api)
		{
			uint platformCount;
			var platformIds = stackalloc nint[32];
			var err = api.GetPlatformIDs(32, platformIds, &platformCount);
			if (err != 0)
				throw new Exception("Unable to get platform ids.");

			if (platformCount == 0)
				throw new Exception("No OpenCL platforms available.");

			var deviceIds = stackalloc nint[32];
			for (var pi = 0; pi < platformCount; pi++)
			{
				uint deviceCount;
				err = api.GetDeviceIDs(platformIds[pi], CLEnum.DeviceTypeGpu, 32, deviceIds, &deviceCount);
				if (err != 0)
					throw new Exception("Unable to get device ids.");

				if (deviceCount == 0)
					continue;

				return (platformIds[pi], deviceIds[0]);
			}

			return default;
		}

		public static unsafe string GetDeviceExtensions(CL api, nint deviceId)
		{
			nuint size;
			var extPtr = stackalloc byte[2048];
			var err = api.GetDeviceInfo(deviceId, (uint)CLEnum.DeviceExtensions, 2048, extPtr, &size);
			if (err != 0)
				throw new Exception("Unable to get device info: CL_DEVICE_EXTENSIONS.");

			var extensions = GetString(extPtr, (int)size);
			return extensions;
		}

		public static unsafe nint CreateContextWithD3D11Sharing(CL api, nint platformId, nint deviceId, DxgiScreenCapture capture)
		{
			var extensions = GetDeviceExtensions(api, deviceId);

			nint* props = default;
			if (extensions.Contains("cl_khr_d3d11_sharing"))
			{
				var p = stackalloc nint[]
				{
					(nint)CLEnum.ContextPlatform, platformId,
					(nint)KHR.ContextD3D11DeviceKhr, capture.Device.NativePointer,
					(nint)CLEnum.ContextInteropUserSync, (nint)CLEnum.False,
					0
				};
				props = p;
			}
			else if (extensions.Contains("cl_nv_d3d11_sharing"))
			{
				var p = stackalloc nint[]
				{
					(nint)CLEnum.ContextPlatform, platformId,
					(nint)NV.CL_CONTEXT_D3D11_DEVICE_NV, capture.Device.NativePointer,
					(nint)CLEnum.ContextInteropUserSync, (nint)CLEnum.False,
					0
				};
				props = p;
			}

			if (props == default)
				throw new Exception("D3D11 sharing is not supported.");

			int err;
			var contextId = api.CreateContext(props, 1, &deviceId, OnCreateContext, null, &err);
			if (err != 0)
				throw new Exception("Unable to create context with D3D11 sharing.");

			return contextId;
		}

		public static unsafe nint CreateContext(CL api, nint platformId, nint deviceId)
		{
			var props = stackalloc nint[]
			{
				(nint)CLEnum.ContextPlatform, platformId,
				0
			};

			int err;
			var contextId = api.CreateContext(props, 1, &deviceId, OnCreateContext, null, &err);
			if (err != 0)
				throw new Exception("Unable to create context with D3D11 sharing.");

			return contextId;
		}

		public static Bitmap CreateBitmap(this CL api, nint queueId, nint imageId)
		{
			return BitmapHelper.CreateBitmap(queueId, imageId);
		}

		private static unsafe void OnCreateContext(byte* errInfo, void* privateInfo, nuint cb, void* userData)
		{
			Console.WriteLine($"Error: {Marshal.PtrToStringAnsi((nint)errInfo)}");
		}
	}
}
