using System;
using System.Runtime.InteropServices;
using Cloo;
using Cloo.Bindings;

namespace Emphasis.OpenCL.Extensions
{
	/// <summary>
	/// Vendor specific extension delegates.
	/// </summary>
	public static partial class OclDelegates
	{
		public delegate ComputeErrorCode clGetDeviceIDsFromD3D11KHR(
			IntPtr platform,
			cl_d3d11_device_source_khr d3dDeviceSource,
			IntPtr d3dObject,
			cl_d3d11_device_set_khr d3dDeviceSet,
			int numEntries,
			[MarshalAs(UnmanagedType.LPArray)] IntPtr[] devices,
			out int numDevices);

		public delegate CLMemoryHandle clCreateFromD3D11Texture2DKHR(
			IntPtr context,
			ComputeMemoryFlags flags,
			IntPtr resource,
			uint subResource,
			out ComputeErrorCode errCode);

		public delegate ComputeErrorCode clEnqueueAcquireD3D11ObjectsKHR(
			CLCommandQueueHandle commandQueue,
			uint numObjects,
			[MarshalAs(UnmanagedType.LPArray)] CLMemoryHandle[] memObjects,
			uint numEventsInWaitList,
			[MarshalAs(UnmanagedType.LPArray)] CLEventHandle[] eventWaitList,
			CLEventHandle @event);

		public delegate ComputeErrorCode clEnqueueReleaseD3D11ObjectsKHR(
			CLCommandQueueHandle commandQueue,
			uint numObjects,
			[MarshalAs(UnmanagedType.LPArray)] CLMemoryHandle[] memObjects,
			uint numEventsInWaitList,
			[MarshalAs(UnmanagedType.LPArray)] CLEventHandle[] eventWaitList,
			CLEventHandle @event);
	}
}