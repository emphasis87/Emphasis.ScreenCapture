using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
		private DxgiScreenCapture GetScreenCapture(IScreenCapture capture)
		{
			if (capture is not DxgiScreenCapture dxgiCapture)
				throw new ArgumentOutOfRangeException(nameof(capture), $"Only {typeof(DxgiScreenCapture)} is supported.");

			return dxgiCapture;
		}

		private static nuint Size<T>(int count) => (nuint) (Marshal.SizeOf<T>() * count);
		private static nuint Size<T>(nint count) => (nuint)(Marshal.SizeOf<T>() * count);
		private static nuint Size<T>(nuint count) => (nuint) (Marshal.SizeOf<T>() * (uint) count);

		private readonly ConcurrentDictionary<nint, Lazy<OclContextInfo>> _contexts = new();
		private readonly ConcurrentDictionary<nint, Lazy<OclPlatformInfo>> _platforms = new();

		private readonly Lazy<CL> _clLazy = new(CL.GetApi);

		public async Task<nint> CreateImage(IScreenCapture capture, nint contextId)
		{
			var api = _clLazy.Value;

			var dxgiCapture = GetScreenCapture(capture);
			
			var context = _contexts.GetOrAdd(contextId, new Lazy<OclContextInfo>(() => CreateContext(contextId))).Value;
			if (context.IsSupportingD3D11Sharing)
			{
				throw new NotSupportedException();
			}

			var exporter = dxgiCapture.ServiceProvider.GetRequiredService<IDxgiScreenCaptureExporter>();

			using var texture = await exporter.MapTexture(dxgiCapture);

			var imageDescription = new ImageDesc(
				(uint)CLEnum.MemObjectImage2D,
				imageWidth: (nuint) dxgiCapture.Width,
				imageHeight: (nuint) dxgiCapture.Height,
				imageRowPitch: (nuint)texture.RowPitch,
				imageSlicePitch: (nuint)texture.SlicePitch);

			unsafe
			{
				var imageFormat = stackalloc uint[2];
				imageFormat[0] = (uint) CLEnum.Bgra;
				imageFormat[1] = (uint) CLEnum.UnsignedInt8;

				int errCreateImage;
				var imageId = api.CreateImage(contextId, CLEnum.MemReadWrite | CLEnum.MemCopyHostPtr, imageFormat, &imageDescription, (void*) texture.DataPointer, &errCreateImage);
				if (errCreateImage != 0)
					throw new ScreenCaptureException($"Unable to create OpenCL image. OpenCL error: {errCreateImage}.");

				return imageId;
			}
		}

		private unsafe OclContextInfo CreateContext(nint contextId)
		{
			var api = _clLazy.Value;

			var context = new OclContextInfo();
			
			nuint size;
			var propsPtr = stackalloc nint[32];
			var errContextInfo = api.GetContextInfo(contextId, (uint) CLEnum.ContextProperties, Size<nint>(32), propsPtr, &size);
			if (errContextInfo != 0)
				return context;

			// Check if the context was created with CL_CONTEXT_D3D11_DEVICE_KHR or CL_
			var props =  new Span<nint>(propsPtr, 32);
			for (var i = 0; i < props.Length; i += 2)
			{
				var propName = props[i];
				if (propName == 0)
					break;

				if (propName == (int) KHR.ContextD3D11DeviceKhr ||
					propName == NV.CL_CONTEXT_D3D11_DEVICE_NV)
				{
					var propValue = props[i + 1];
					if (propValue != 0)
					{
						context.IsSupportingD3D11Sharing = true;
						context.D3D11DeviceId = propValue;
					}
					
					break;
				}
			}

			if (!context.IsSupportingD3D11Sharing)
				return context;
			
			nint platformId;
			var errDeviceInfo = api.GetDeviceInfo(context.D3D11DeviceId, (uint)CLEnum.DevicePlatform, Size<nint>(1), &platformId, &size);
			if (errDeviceInfo != 0)
			{
				context.IsSupportingD3D11Sharing = false;
				return context;
			}

			var platformId2 = platformId;
			var platform = _platforms.GetOrAdd(platformId, new Lazy<OclPlatformInfo>(() => CreatePlatform(platformId2))).Value;

			context.IsSupportingD3D11Sharing &= platform.IsSupportingD3D11Sharing;

			return context;
		}

		private unsafe OclPlatformInfo CreatePlatform(nint platformId)
		{
			var api = _clLazy.Value;

			var info = new OclPlatformInfo();

			nuint size;
			var extPtr = stackalloc byte[2048];
			var errPlatformInfo = api.GetPlatformInfo(platformId, (uint)CLEnum.DeviceExtensions, 2048, extPtr, &size);
			if (errPlatformInfo != 0)
				return info;

			var ext = new Span<byte>(extPtr, 2048);
			var extensions = Encoding.ASCII.GetString(ext.ToArray(), 0, (int)size);
			if (extensions.Contains("cl_khr_d3d11_sharing"))
			{
				var getDevicesPtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clGetDeviceIDsFromD3D11KHR");
				if (getDevicesPtr == default)
					return info;
				
				info.GetDeviceIDs = Marshal.GetDelegateForFunctionPointer<OclDelegates.clGetDeviceIDsFromD3D11>(getDevicesPtr);

				var createFromTexturePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clCreateFromD3D11Texture2DKHR");
				if (createFromTexturePtr == default)
					return info;

				info.CreateFromTexture2D = Marshal.GetDelegateForFunctionPointer<OclDelegates.clCreateFromD3D11Texture2D>(getDevicesPtr);
				info.IsSupportingD3D11Sharing = true;
			}
			else if (extensions.Contains("cl_nv_d3d11_sharing"))
			{
				var getDevicesPtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clGetDeviceIDsFromD3D11NV");
				if (getDevicesPtr == default)
					return info;

				info.GetDeviceIDs = Marshal.GetDelegateForFunctionPointer<OclDelegates.clGetDeviceIDsFromD3D11>(getDevicesPtr);

				var createFromTexturePtr = (nint)api.GetExtensionFunctionAddressForPlatform(platformId, "clCreateFromD3D11Texture2DNV");
				if (createFromTexturePtr == default)
					return info;

				info.CreateFromTexture2D = Marshal.GetDelegateForFunctionPointer<OclDelegates.clCreateFromD3D11Texture2D>(getDevicesPtr);
				info.IsSupportingD3D11Sharing = true;
			}

			return info;
		}

		

		public Task<nint> CreateOrCopyToImage(IScreenCapture capture, nint clContext, nint clImage)
		{
			throw new NotImplementedException();
		}

		public Task<nint> CopyToImage(IScreenCapture capture, nint clContext, nint clImage)
		{
			throw new NotImplementedException();
		}

		public nint CreateImage(IScreenCapture capture, nint contextId, nint clEvent)
		{
			throw new NotImplementedException();
		}

		public nint CreateOrCopyToImage(IScreenCapture capture, nint clContext, nint clImage, nint clEvent)
		{
			throw new NotImplementedException();
		}

		public nint CopyToImage(IScreenCapture capture, nint clContext, nint clImage, nint clEvent)
		{
			throw new NotImplementedException();
		}
	}
}