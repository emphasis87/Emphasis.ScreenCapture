using System;
using System.Runtime.InteropServices;
using Cloo;
using Cloo.Bindings;

namespace Emphasis.OpenCL
{
	/// <summary>
	/// Vendor specific extension delegates.
	/// </summary>
	public static partial class OclDelegates
	{
		
		public delegate ComputeErrorCode clGetDeviceIDsFromD3D11NV(
			IntPtr platform,
			cl_d3d11_device_source_nv d3dDeviceSource,
			IntPtr d3dObject,
			cl_d3d11_device_set_nv d3dDeviceSet,
			int numEntries,
			[MarshalAs(UnmanagedType.LPArray)] IntPtr[] devices,
			out int numDevices);

		public delegate CLMemoryHandle clCreateFromD3D11Texture2DNV(
			IntPtr context,
			ComputeMemoryFlags flags,
			IntPtr resource,
			uint subResource,
			out ComputeErrorCode errCode);

		public delegate ComputeErrorCode clEnqueueAcquireD3D11ObjectsNV(
			CLCommandQueueHandle commandQueue,
			uint numObjects,
			[MarshalAs(UnmanagedType.LPArray)] CLMemoryHandle[] memObjects,
			uint numEventsInWaitList,
			[MarshalAs(UnmanagedType.LPArray)] CLEventHandle[] eventWaitList,
			CLEventHandle @event);

		public delegate ComputeErrorCode clEnqueueReleaseD3D11ObjectsNV(
			CLCommandQueueHandle commandQueue,
			uint numObjects,
			[MarshalAs(UnmanagedType.LPArray)] CLMemoryHandle[] memObjects,
			uint numEventsInWaitList,
			[MarshalAs(UnmanagedType.LPArray)] CLEventHandle[] eventWaitList,
			CLEventHandle @event);

		/*
		cl_int clGetDeviceIDsFromD3D11NV(
			cl_platform_id platform,
			cl_d3d11_device_source_nv d3d_device_source,
			void* d3d_object,
			cl_d3d11_device_set_nv d3d_device_set,
			cl_uint num_entries,
			cl_device_id* devices,
			cl_uint* num_devices)

		cl_mem clCreateFromD3D11Texture2DNV(
			cl_context context,
			cl_mem_flags flags,
			ID3D11Texture2D* resource,
			UINT subresource,
			cl_int* errcode_ret)

		cl_int clEnqueueAcquireD3D11ObjectsNV(
			cl_command_queue command_queue,
			cl_uint num_objects,
			const cl_mem* mem_objects,
			cl_uint num_events_in_wait_list,
			const cl_event* event_wait_list,
			cl_event *event)

		cl_int clEnqueueReleaseD3D11ObjectsNV(
			cl_command_queue command_queue,
			cl_uint num_objects,
			cl_mem* mem_objects,
			cl_uint num_events_in_wait_list,
			const cl_event* event_wait_list,
			cl_event *event)
		*/
	}
}