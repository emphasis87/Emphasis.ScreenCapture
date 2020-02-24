using System.Linq;
using System.Threading.Tasks;
using Cloo;
using Emphasis.ScreenCapture.Windows.Dxgi;
using SharpDX.Direct3D11;

namespace Emphasis.ScreenCapture.OpenCL
{
	public class ComputeMemoryDispatcher
	{
		public async Task<ComputeMemory> Dispatch(ScreenCapture capture, ComputeContext context)
		{
			if (capture is DxgiScreenCapture dxgiScreenCapture && capture.Method is IDxgiScreenCaptureMethod method)
			{
				if (context.Platform.Extensions.Contains("cl_khr_d3d11_sharing"))
				{
					
				}
				else if (context.Platform.Extensions.Contains("cl_nv_d3d11_sharing"))
				{

				}

				var texture = await method.MapTexture(dxgiScreenCapture);
				var size = capture.Height * capture.Width * 4;

#pragma warning disable CS0618 // Type or member is obsolete
				return new ComputeImage2D(
					context,
					ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer,
					new ComputeImageFormat(ComputeImageChannelOrder.Bgra, ComputeImageChannelType.UNormInt8),
					capture.Width,
					capture.Height,
					0,
					texture.DataPointer);
#pragma warning restore CS0618 // Type or member is obsolete

				//return new ComputeBuffer<byte>(context, flags, size, texture.DataPointer);
			}

			return null;
		}
	}
}
