using Cloo;
using Emphasis.ScreenCapture.Windows.Dxgi;
using SharpDX.Direct3D11;

namespace Emphasis.ScreenCapture.OpenCL
{
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
					// https://github.com/KhronosGroup/OpenCL-Headers/blob/master/CL/cl_d3d11.h
				}
				else if (context.Platform.Extensions.Contains("cl_nv_d3d11_sharing"))
				{
					// https://www.khronos.org/registry/OpenCL/extensions/nv/cl_nv_d3d11_sharing.txt
					// http://developer.download.nvidia.com/compute/cuda/3_0/toolkit/docs/opencl_extensions/cl_nv_d3d11_sharing.txt
					// https://github.com/sschaetz/nvidia-opencl-examples/blob/master/OpenCL/common/inc/CL/cl_d3d11_ext.h
				}
			}

			return null;
		}
	}
}
