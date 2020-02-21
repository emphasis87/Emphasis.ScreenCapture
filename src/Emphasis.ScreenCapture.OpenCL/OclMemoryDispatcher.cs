using System;
using System.Runtime.InteropServices;
using Cloo;
using Cloo.Bindings;
using Emphasis.ScreenCapture.Windows.Dxgi;
using SharpDX.Direct3D11;

namespace Emphasis.ScreenCapture.OpenCL
{
	public enum cl_d3d11_device_source_khr
	{
		CL_D3D11_DEVICE_KHR,
		CL_D3D11_DXGI_ADAPTER_KHR
	}

	public enum cl_d3d11_device_set_khr
	{
		CL_PREFERRED_DEVICES_FOR_D3D11_KHR,
		CL_ALL_DEVICES_FOR_D3D11_KHR
	}

	public enum cl_d3d11_device_source_nv
	{
		CL_D3D11_DEVICE_NV = 0x4019,
		CL_D3D11_DXGI_ADAPTER_NV = 0x401A,
	}

	public enum cl_d3d11_device_set_nv
	{
		CL_PREFERRED_DEVICES_FOR_D3D11_NV = 0x401B,
		CL_ALL_DEVICES_FOR_D3D11_NV = 0x401C,
	}

	public static class CL12x
	{
		[DllImport("OpenCL", EntryPoint = "clGetExtensionFunctionAddressForPlatform ")]
		public static extern IntPtr clGetExtensionFunctionAddressForPlatform(IntPtr platform, string func_name);

		public static bool TryFindClGetDeviceIDsFromD3D11KHR(
			ComputePlatform platform, 
			out OclDelegates.clGetDeviceIDsFromD3D11KHR getDeviceIdsFromD3D11KHR)
		{
			getDeviceIdsFromD3D11KHR = null;
			if (!platform.Extensions.Contains("cl_khr_d3d11_sharing"))
				return false;

			var handler = CL12.GetExtensionFunctionAddressForPlatform(platform.Handle, "clGetDeviceIDsFromD3D11KHR");
			if (handler == IntPtr.Zero)
				return false;

			getDeviceIdsFromD3D11KHR = (OclDelegates.clGetDeviceIDsFromD3D11KHR) 
				Marshal.GetDelegateForFunctionPointer(handler, typeof(OclDelegates.clGetDeviceIDsFromD3D11KHR));

			return true;
		}

		public static bool TryFindClGetDeviceIDsFromD3D11NV(
			ComputePlatform platform,
			out OclDelegates.clGetDeviceIDsFromD3D11NV getDeviceIdsFromD3D11NV)
		{
			getDeviceIdsFromD3D11NV = null;
			if (!platform.Extensions.Contains("cl_nv_d3d11_sharing"))
				return false;

			var handler = CL12.GetExtensionFunctionAddressForPlatform(platform.Handle, "clGetDeviceIDsFromD3D11NV");
			if (handler == IntPtr.Zero)
				return false;

			getDeviceIdsFromD3D11NV = (OclDelegates.clGetDeviceIDsFromD3D11NV)
				Marshal.GetDelegateForFunctionPointer(handler, typeof(OclDelegates.clGetDeviceIDsFromD3D11NV));

			return true;
		}
	}

	public class OclMemoryDispatcher
	{
		public ComputeBuffer<T> Dispatch<T>(ScreenCapture capture, ComputeContext context, ComputeMemoryFlags flags)
			where T : struct
		{
			if (capture is DxgiScreenCapture dxgiScreenCapture)
			{
				if (context.Platform.Extensions.Contains("cl_khr_d3d11_sharing"))
				{
					// https://www.khronos.org/registry/OpenCL/sdk/1.2/docs/man/xhtml/cl_khr_d3d11_sharing.html
				}
				else if (context.Platform.Extensions.Contains("cl_nv_d3d11_sharing"))
				{
					// https://www.khronos.org/registry/OpenCL/extensions/nv/cl_nv_d3d11_sharing.txt
					// http://developer.download.nvidia.com/compute/cuda/3_0/toolkit/docs/opencl_extensions/cl_nv_d3d11_sharing.txt
					// https://github.com/sschaetz/nvidia-opencl-examples/blob/master/OpenCL/common/inc/CL/cl_d3d11_ext.h
				}
			}


			//if (context.Platform.Extensions.Contains("khr-d3d-sharing"))
			//{
			//	if (capture is DxgiScreenCapture dxgiCapture)
			//	{
			//		dxgiCapture.
			//	}
			//}
			
			//var data = capture.GetBytes();
			//return new ComputeBuffer<T>(context, flags, )
			return null;
		}
	}
}
