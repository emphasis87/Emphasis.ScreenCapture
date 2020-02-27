using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
		Task<DxgiDataBox> MapTexture(DxgiScreenCapture capture);

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

			IDisposable cleanup = null;
			DxgiScreenCapture capture = null;
			while (!cancellationToken.IsCancellationRequested)
			{
				OutputDuplicateFrameInformation frameInformation = default;
				Resource screenResource = default;
				try
				{
					var result = await Task.Run(() => 
					outputDuplication.TryAcquireNextFrame(1000, out frameInformation, out screenResource), cancellationToken);

					cleanup = new CompositeDisposable(
					new[] { screenResource, Disposable.Create(outputDuplication.ReleaseFrame) }.Where(x => x != null));
				
					if (frameInformation.AccumulatedFrames == 0)
					{
						cleanup.Dispose();
						continue;
					}

					if (result != Result.Ok)
						yield break;

					capture = new DxgiScreenCapture(screen, DateTime.Now, width, height, this, adapter, output1,
						device, outputDuplication, screenResource, frameInformation);

					capture.Add(cleanup);

					yield return capture;

				}
				finally
				{
					cleanup?.Dispose();
					capture?.Dispose();
				}
			}
		}

		public async Task<DxgiDataBox> MapTexture(DxgiScreenCapture capture)
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
				device.ImmediateContext.MapSubresource(targetTexture, 0, MapMode.Read, MapFlags.None));

			var result = new DxgiDataBox(data.DataPointer, data.RowPitch, data.SlicePitch);

			result.Add(
				new CompositeDisposable(
					Disposable.Create(() =>
						device.ImmediateContext.UnmapSubresource(targetTexture, 0)),
					targetTexture));

			return result;
		}

		public async Task<Bitmap> ToBitmap(DxgiScreenCapture capture)
		{
			var width = capture.Width;
			var height = capture.Height;

			using var texture = await MapTexture(capture);

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

		public async Task<byte[]> ToBytes(DxgiScreenCapture capture)
		{
			var width = capture.Width;
			var height = capture.Height;

			using var texture = await MapTexture(capture);
			var sourcePointer = texture.DataPointer;

			var target = new byte[height * width * 4];
			var targetHandle = GCHandle.Alloc(target, GCHandleType.Pinned);
			var targetPointer = targetHandle.AddrOfPinnedObject();

			for (var y = 0; y < height; y++)
			{
				Utilities.CopyMemory(targetPointer, sourcePointer, width * 4);

				sourcePointer = IntPtr.Add(sourcePointer, texture.RowPitch);
				targetPointer = IntPtr.Add(targetPointer, width * 4);
			}

			targetHandle.Free();

			return target;
		}
	}
}