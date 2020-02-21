using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Rectangle = SharpDX.Rectangle;
using Resource = SharpDX.DXGI.Resource;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public interface IDxgiScreenCaptureMethod : IScreenCaptureMethod
	{
		Task<Bitmap> ToBitmap(DxgiScreenCapture capture);
	}

	public class DxgiScreenCaptureMethod : IDxgiScreenCaptureMethod
	{
		public async IAsyncEnumerable<ScreenCapture> Capture(
			Screen screen,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var factory = new Factory1();
			using var adapter = factory.Adapters1.FirstOrDefault(x => x.Description.DeviceId == screen.AdapterId);
			if (adapter == null)
				throw new ArgumentOutOfRangeException(nameof(screen), screen.AdapterId, $"Unable to find an adapter width the id: {screen.AdapterId}.");
			
			using var output = adapter.Outputs.FirstOrDefault(x => x.Description.DeviceName == screen.OutputName);
			if (output == null)
				throw new ArgumentOutOfRangeException(nameof(screen), screen.OutputName, $"Unable to find an output with the name: {screen.OutputName}.");

			using var device = new Device(adapter);
			using var output1 = output.QueryInterface<Output1>();
			using var outputDuplication = output1.DuplicateOutput(device);

			var bounds = (Rectangle)output.Description.DesktopBounds;
			var width = bounds.Width;
			var height = bounds.Height;

			while (!cancellationToken.IsCancellationRequested)
			{
				OutputDuplicateFrameInformation frameInformation = default;
				Resource screenResource = default;
				
				var result = await Task.Run(() => 
					outputDuplication.TryAcquireNextFrame(1000, out frameInformation, out screenResource), cancellationToken);

				using var cleanup = new CompositeDisposable(
					new[] { screenResource, Disposable.Create(outputDuplication.ReleaseFrame) }.Where(x => x != null));
				
				if (frameInformation.AccumulatedFrames == 0)
				{
					cleanup.Dispose();
					continue;
				}

				if (result != Result.Ok)
					yield break;

				var capture = new DxgiScreenCapture(screen, DateTime.Now, width, height, this, adapter, output1,
					device, outputDuplication, screenResource, frameInformation);
				yield return capture;

				cleanup.Dispose();
			}
		}

		public async Task<Bitmap> ToBitmap(DxgiScreenCapture capture)
		{
			var width = capture.Width;
			var height = capture.Height;
			var device = capture.Device;
			var screenResource = capture.ScreenResource;

			// Create Staging texture CPU-accessible
			var textureDesc = new Texture2DDescription
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
			var screenTexture = new Texture2D(device, textureDesc);

			// Copy resource into memory that can be accessed by the CPU
			using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
			{
				device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
			}

			// Get the desktop capture texture
			var mapSource = await Task.Run(() =>
				device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, MapFlags.None));

			// Create Drawing.Bitmap
			var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

			// Copy pixels from screen capture Texture to GDI bitmap
			var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
			var sourcePtr = mapSource.DataPointer;
			var destPtr = mapDest.Scan0;
			for (var y = 0; y < height; y++)
			{
				// Copy a single line 
				Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

				// Advance pointers
				sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
				destPtr = IntPtr.Add(destPtr, mapDest.Stride);
			}

			// Release source and dest locks
			bitmap.UnlockBits(mapDest);
			device.ImmediateContext.UnmapSubresource(screenTexture, 0);

			return bitmap;
		}
	}
}