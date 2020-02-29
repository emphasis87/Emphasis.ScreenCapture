using System.Linq;
using System.Threading.Tasks;
using Cloo;
using Emphasis.OpenCL.Helpers;
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

				// Map the texture into the main memory
				var texture = await method.MapTexture(dxgiScreenCapture);

				var image = context.CreateImage2D(texture.DataPointer, capture.Width, capture.Height);
				
				// Dispose of the texture after the image is disposed
				image.Add(texture);

				return image;
			}

			return null;
		}
	}
}
