using System;
using System.Runtime.InteropServices;
using Cloo;

namespace Emphasis.ScreenCapture.OpenCL
{
	/// <summary>
	/// Vendor specific extension delegates.
	/// </summary>
	public static class OclDelegates
	{
		public delegate ComputeErrorCode clGetDeviceIDsFromD3D11KHR(
			IntPtr in_platform,
			cl_d3d11_device_source_khr in_d3d_device_source,
			IntPtr in_d3d_object,
			cl_d3d11_device_set_khr in_d3d_device_set,
			int num_entries,
			[MarshalAs(UnmanagedType.LPArray)] IntPtr[] out_devices,
			out int num_devices);

		public delegate ComputeErrorCode clGetDeviceIDsFromD3D11NV(
			IntPtr in_platform,
			cl_d3d11_device_source_nv d3d_device_source,
			IntPtr in_d3d_object,
			cl_d3d11_device_set_nv d3d_device_set,
			int num_entries,
			[MarshalAs(UnmanagedType.LPArray)] IntPtr[] out_devices,
			out int num_devices);
	}
}