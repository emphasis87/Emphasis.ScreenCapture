using System;
using System.Runtime.InteropServices;
using Cloo;
using Cloo.Bindings;

namespace Emphasis.OpenCL
{
	public static partial class CL12x
	{
		/// <summary>
		/// https://www.khronos.org/registry/OpenCL/extensions/nv/cl_nv_d3d11_sharing.txt
		/// http://developer.download.nvidia.com/compute/cuda/3_0/toolkit/docs/opencl_extensions/cl_nv_d3d11_sharing.txt
		/// https://github.com/sschaetz/nvidia-opencl-examples/blob/master/OpenCL/common/inc/CL/cl_d3d11_ext.h
		/// </summary>
		/// <param name="platform"></param>
		/// <param name="getDeviceIdsFromD3D11NV"></param>
		/// <returns></returns>
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
}