using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiScreenCaptureExporter : IScreenCaptureBitmapFactory
	{
		public async Task<Bitmap> ToBitmap(IScreenCapture capture)
		{
			if (!(capture is DxgiScreenCapture dxgiCapture))
				throw new ArgumentOutOfRangeException(nameof(capture), $"Only {typeof(DxgiScreenCapture)} is supported.");

			var width = dxgiCapture.Width;
			var height = dxgiCapture.Height;

			using var texture = await MapTexture(dxgiCapture);

			// Create Drawing.Bitmap
			var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

			// Copy pixels from screen capture Texture to GDI bitmap
			var mapTarget = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
			var sourcePtr = texture.DataPointer;
			var targetPtr = mapTarget.Scan0;
			for (var y = 0; y < height; y++)
			{
				// Copy a single line 
				Utilities.CopyMemory(targetPtr, sourcePtr, width * 4);

				// Advance pointers
				sourcePtr = IntPtr.Add(sourcePtr, texture.RowPitch);
				targetPtr = IntPtr.Add(targetPtr, mapTarget.Stride);
			}

			// Release source and dest locks
			bitmap.UnlockBits(mapTarget);

			return bitmap;
		}

		internal static async Task<DxgiTexture> MapTexture(DxgiScreenCapture capture)
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

			result.Add(
				new CompositeDisposable(
					Disposable.Create(() => device.ImmediateContext.UnmapSubresource(targetTexture, 0)),
					targetTexture));

			return result;
		}
	}
}
