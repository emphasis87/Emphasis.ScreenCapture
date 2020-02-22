﻿using System;
using System.Runtime.InteropServices;
using Cloo;
using Cloo.Bindings;

namespace Emphasis.OpenCL
{
	public static partial class CL12x
	{
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
	}

	public enum cl_d3d11_device_source_khr
	{
		CL_D3D11_DEVICE_KHR = 0x4019,
		CL_D3D11_DXGI_ADAPTER_KHR = 0x401A
	}

	public enum cl_d3d11_device_set_khr
	{
		CL_PREFERRED_DEVICES_FOR_D3D11_KHR = 0x401B,
		CL_ALL_DEVICES_FOR_D3D11_KHR = 0x401C
	}
}