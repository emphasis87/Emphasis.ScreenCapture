using System;
using System.Runtime.InteropServices;
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
