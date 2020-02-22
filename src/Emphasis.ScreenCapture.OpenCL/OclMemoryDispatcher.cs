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
					
				}
				else if (context.Platform.Extensions.Contains("cl_nv_d3d11_sharing"))
				{

				}
			}

			return null;
		}
	}
}
