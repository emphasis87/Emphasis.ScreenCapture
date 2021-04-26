using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SharpDX.Direct3D11;
using SharpDX.Text;
using Silk.NET.OpenCL;
using Silk.NET.OpenCL.Extensions.KHR;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI.OpenCL
{
	public interface IDxgiScreenCaptureOclImageFactory : IScreenCaptureOclImageFactory
	{

	}

	internal static class NV
	{
		public const int CL_CONTEXT_D3D11_DEVICE_NV = 0x401D;
	}

	public class DxgScreenCaptureOclImageFactory : IDxgiScreenCaptureOclImageFactory
	{
		private readonly ConcurrentDictionary<nint, Lazy<OclContextInfo>> _contexts = new();
		private readonly ConcurrentDictionary<nint, Lazy<OclPlatformInfo>> _platforms = new();

		private readonly Lazy<CL> _clLazy = new(CL.GetApi);

		private OclContextInfo GetContextInfo(nint contextId) => _contexts.GetOrAdd(contextId, new Lazy<OclContextInfo>(() => CreateContext(contextId))).Value;
		private OclPlatformInfo GetPlatformInfo(nint platformId) => _platforms.GetOrAdd(platformId, new Lazy<OclPlatformInfo>(() => CreatePlatform(platformId))).Value;

		private static DxgiScreenCapture GetScreenCapture(IScreenCapture capture)
		{
			if (capture is not DxgiScreenCapture dxgiCapture)
				throw new ArgumentOutOfRangeException(nameof(capture), $"Only {typeof(DxgiScreenCapture)} is supported.");

			return dxgiCapture;
		}

		private static nuint Size<T>(int count) => (nuint)(Marshal.SizeOf<T>() * count);

		public async Task<IOclImage> CreateImage(IScreenCapture capture, nint contextId, nint queueId)
		{
			var dxgiCapture = GetScreenCapture(capture);

			int err;

			unsafe
			{
				var context = GetContextInfo(contextId);
				if (!context.IsSupportingD3D11Sharing)
					goto createFromHostPtr;

				var platform = GetPlatformInfo(context.PlatformId);
				var sourceTexture = dxgiCapture.ScreenResource.QueryInterface<Texture2D>();
				dxgiCapture.Add(sourceTexture);
				var imageId = platform.CreateFromTexture2D(contextId, (uint) CLEnum.MemReadWrite, sourceTexture.NativePointer, 0, &err);
				if (err != 0)
					goto createFromHostPtr;

				var image = new OclImage(imageId, queueId, platform);
				return image;
			}

			createFromHostPtr:
			return await CreateImageFromHostPtr(dxgiCapture, contextId);
		}
		
		private async Task<IOclImage> CreateImageFromHostPtr(DxgiScreenCapture dxgiCapture, nint contextId)
		{
			var api = _clLazy.Value;

			var exporter = dxgiCapture.ServiceProvider.GetRequiredService<IDxgiScreenCaptureExporter>();

			nint imageId;

			var texture = await exporter.MapTexture(dxgiCapture);
			try
			{
				var imageDescription = new ImageDesc(
					(uint) CLEnum.MemObjectImage2D,
					imageWidth: (nuint) dxgiCapture.Width,
					imageHeight: (nuint) dxgiCapture.Height,
					imageRowPitch: (nuint) texture.RowPitch,
					imageSlicePitch: (nuint) texture.SlicePitch);

				unsafe
				{
					var imageFormat = stackalloc uint[2];
					imageFormat[0] = (uint) CLEnum.Bgra;
					imageFormat[1] = (uint) CLEnum.UnsignedInt8;

					int err;
					imageId = api.CreateImage(contextId, CLEnum.MemCopyHostPtr, imageFormat, &imageDescription, (void*) texture.DataPointer, &err);
					if (err != 0)
						throw new ScreenCaptureException($"Unable to create OpenCL image. OpenCL error: {err}.");
				}
			}
			finally
			{
				texture.Dispose(); 
			}

			return new OclImage(imageId);
		}

		private unsafe OclContextInfo CreateContext(nint contextId)
		{
			var api = _clLazy.Value;

			var context = new OclContextInfo();

			int err;

			nuint size;
			var propsPtr = stackalloc nint[32];
			err = api.GetContextInfo(contextId, (uint) CLEnum.ContextProperties, Size<nint>(32), propsPtr, &size);
			if (err != 0)
				return context;
			
			var props =  new Span<nint>(propsPtr, 32);
			for (var i = 0; i < props.Length; i += 2)
			{
				var propName = props[i];
				if (propName == 0)
					break;

				var propValue = props[i + 1];

				// Check if the context was created D3D11Device
				if (propName == (int) KHR.ContextD3D11DeviceKhr ||
					propName == NV.CL_CONTEXT_D3D11_DEVICE_NV)
				{
					if (propValue != 0)
						context.D3D11DeviceId = propValue;
				}

				// Check if user handles interop synchronization
				if (propName == (int) CLEnum.ContextInteropUserSync)
				{
					if (propValue == (int) CLEnum.True)
						context.InteropUserSync = true;
				}
			}

			if (context.D3D11DeviceId == default)
				return context;

			var devices = stackalloc nint[32];
			err = api.GetContextInfo(contextId, (uint) CLEnum.ContextDevices, Size<nint>(32), devices, out var devSize);
			if (err != 0)
				return context;

			var deviceId = devices[0];
			nint platformId;
			err = api.GetDeviceInfo(deviceId, (uint)CLEnum.DevicePlatform, Size<nint>(1), &platformId, &size);
			if (err != 0)
				return context;
			
			var platform = GetPlatformInfo(platformId);
			context.PlatformId = platformId;
			context.IsSupportingD3D11Sharing = platform.IsSupportingD3D11Sharing;

			return context;
		}

		private unsafe OclPlatformInfo CreatePlatform(nint platformId)
		{
			var api = _clLazy.Value;

			var info = new OclPlatformInfo();

			int err;

			nuint size;
			var extPtr = stackalloc byte[2048];
			err = api.GetPlatformInfo(platformId, (uint)CLEnum.PlatformExtensions, 2048, extPtr, &size);
			if (err != 0)
				return info;

			var ext = new Span<byte>(extPtr, 2048);
			var extensions = Encoding.ASCII.GetString(ext.ToArray(), 0, (int)size);
			if (extensions.Contains("cl_khr_d3d11_sharing"))
			{
				var getDevicesPtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clGetDeviceIDsFromD3D11KHR");
				if (getDevicesPtr == IntPtr.Zero)
					return info;
				info.GetDeviceIDs = Marshal.GetDelegateForFunctionPointer<OclDelegates.clGetDeviceIDsFromD3D11>(getDevicesPtr);

				var createFromTexturePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clCreateFromD3D11Texture2DKHR");
				if (createFromTexturePtr == IntPtr.Zero)
					return info;
				info.CreateFromTexture2D = Marshal.GetDelegateForFunctionPointer<OclDelegates.clCreateFromD3D11Texture2D>(createFromTexturePtr);

				var enqueueAcquirePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clEnqueueAcquireD3D11ObjectsKHR");
				if (enqueueAcquirePtr == IntPtr.Zero)
					return info;
				info.EnqueueAcquireD3D11Objects = Marshal.GetDelegateForFunctionPointer<OclDelegates.clEnqueueAcquireD3D11Objects>(enqueueAcquirePtr);

				var enqueueReleasePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clEnqueueReleaseD3D11ObjectsKHR");
				if (enqueueReleasePtr == IntPtr.Zero)
					return info;
				info.EnqueueReleaseD3D11Objects = Marshal.GetDelegateForFunctionPointer<OclDelegates.clEnqueueReleaseD3D11Objects>(enqueueReleasePtr);

				info.IsSupportingD3D11Sharing = true;
			}
			else if (extensions.Contains("cl_nv_d3d11_sharing"))
			{
				var getDevicesPtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clGetDeviceIDsFromD3D11NV");
				if (getDevicesPtr == IntPtr.Zero)
					return info;
				info.GetDeviceIDs = Marshal.GetDelegateForFunctionPointer<OclDelegates.clGetDeviceIDsFromD3D11>(getDevicesPtr);

				var createFromTexturePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clCreateFromD3D11Texture2DNV");
				if (createFromTexturePtr == IntPtr.Zero)
					return info;
				info.CreateFromTexture2D = Marshal.GetDelegateForFunctionPointer<OclDelegates.clCreateFromD3D11Texture2D>(createFromTexturePtr);

				var enqueueAcquirePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clEnqueueAcquireD3D11ObjectsNV");
				if (enqueueAcquirePtr == IntPtr.Zero)
					return info;
				info.EnqueueAcquireD3D11Objects = Marshal.GetDelegateForFunctionPointer<OclDelegates.clEnqueueAcquireD3D11Objects>(enqueueAcquirePtr);

				var enqueueReleasePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clEnqueueReleaseD3D11ObjectsNV");
				if (enqueueReleasePtr == IntPtr.Zero)
					return info;
				info.EnqueueReleaseD3D11Objects = Marshal.GetDelegateForFunctionPointer<OclDelegates.clEnqueueReleaseD3D11Objects>(enqueueReleasePtr);

				info.IsSupportingD3D11Sharing = true;
			}

			return info;
		}
	}
}