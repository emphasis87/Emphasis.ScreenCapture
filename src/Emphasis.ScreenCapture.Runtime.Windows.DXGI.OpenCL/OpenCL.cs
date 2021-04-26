using System;
using System.Collections.Generic;
using System.Text;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI.OpenCL
{
	

	internal class OclContextInfo
	{
		public bool IsSupportingD3D11Sharing { get; set; }
		public nint D3D11DeviceId { get; set; }
		public nint PlatformId { get; set; }
		public nint[] Devices { get; set; }
	}

	internal class OclPlatformInfo
	{
		public bool IsSupportingD3D11Sharing { get; set; }
		public OclDelegates.clGetDeviceIDsFromD3D11 GetDeviceIDs { get; set; }
		public OclDelegates.clCreateFromD3D11Texture2D CreateFromTexture2D { get; set; }
	}

	internal static class OclDelegates
	{
		public unsafe delegate int clGetDeviceIDsFromD3D11(
			nint platform,
			uint d3dDeviceSource,
			nint d3dObject,
			uint d3dDeviceSet,
			int numEntries,
			nint* devices,
			uint* numDevices);

		public unsafe delegate nint clCreateFromD3D11Texture2D (
			nint context,
			uint flags,
			nint resource,
			uint subResource,
			int* errCode);
	}
}
