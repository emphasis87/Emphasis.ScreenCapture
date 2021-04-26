using System.Reactive.Disposables;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public interface IDxgiScreenCaptureExporter
	{
		Task<DxgiTexture> MapTexture(DxgiScreenCapture capture);
	}

	public class DxgiScreenCaptureExporter : IDxgiScreenCaptureExporter
	{
		public async Task<DxgiTexture> MapTexture(DxgiScreenCapture capture)
		{
			var width = capture.Width;
			var height = capture.Height;
			var device = capture.Device;
			var screenResource = capture.ScreenResource;

			// Create Staging texture CPU-accessible
			var textureDescription = new Texture2DDescription
			{
				CpuAccessFlags = CpuAccessFlags.Read,
				BindFlags = BindFlags.None,
				Format = Format.B8G8R8A8_UNorm,
				Width = width,
				Height = height,
				OptionFlags = ResourceOptionFlags.None,
				MipLevels = 1,
				ArraySize = 1,
				SampleDescription = { Count = 1, Quality = 0 },
				Usage = ResourceUsage.Staging
			};
			var targetTexture = new Texture2D(device, textureDescription);

			// Copy resource into memory that can be accessed by the CPU
			using (var sourceTexture = screenResource.QueryInterface<Texture2D>())
			{
				await Task.Run(() =>
					device.ImmediateContext.CopyResource(sourceTexture, targetTexture));
			}

			// Get the desktop capture texture
			var data = await Task.Run(() =>
				device.ImmediateContext.MapSubresource(targetTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None));

			var result = new DxgiTexture(data.DataPointer, data.RowPitch, data.SlicePitch);

			result.Add(Disposable.Create(() => 
				device.ImmediateContext.UnmapSubresource(targetTexture, 0)));
			result.Add(targetTexture);

			return result;
		}
	}
}
