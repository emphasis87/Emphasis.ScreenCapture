using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public interface IDxgiScreenCaptureMethod : IScreenCaptureMethod, IScreenCaptureBitmapFactory, IScreenProvider
	{

	}

	public class DxgiScreenCaptureMethod : IDxgiScreenCaptureMethod
	{
		public IScreen[] GetScreens()
		{
			using var factory = new Factory1();
			var adapters = factory.Adapters1;
			var screens =
				adapters.SelectMany(adapter =>
					adapter.Outputs.Select(output =>
						(IScreen)new Screen(adapter.Description.DeviceId, output.Description.DeviceName))
				).ToArray();
			return screens;
		}

		public async Task<IScreenCapture> Capture(IScreen screen, CancellationToken cancellationToken = default)
		{
			var stream = CaptureStream(screen, cancellationToken);
			return await stream.FirstOrDefaultAsync(cancellationToken);
		}

		public async IAsyncEnumerable<IScreenCapture> CaptureStream(IScreen screen, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

			OutputDuplicateFrameInformation frameInformation = default;
			Resource screenResource = default;
			DxgiScreenCapture capture = default;
			CompositeDisposable cleanup = default;
			var result = Result.Ok;
			
			while (!cancellationToken.IsCancellationRequested)
			{
				cleanup = new CompositeDisposable();

				try
				{
					result = await Task.Run(() =>
							outputDuplication.TryAcquireNextFrame(1000, out frameInformation, out screenResource),
						cancellationToken);

					if (screenResource != null)
						cleanup.Add(screenResource);

					cleanup.Add(Disposable.Create(outputDuplication.ReleaseFrame));

					if (frameInformation.AccumulatedFrames == 0)
					{
						Cleanup();
						continue;
					}

					if (result != Result.Ok)
						break;

					capture = new DxgiScreenCapture(screen, this, DateTime.Now, width, height, adapter, output1,
						device, outputDuplication, screenResource, frameInformation);

					capture.Add(cleanup);

					yield return capture;

				}
				finally
				{
					Cleanup();
				}
			}

			Cleanup();

			if (result != Result.Ok)
				throw new ScreenCaptureException($"{typeof(OutputDuplication)}.{nameof(OutputDuplication.TryAcquireNextFrame)} returned error: {result}.");

			void Cleanup()
			{
				cleanup?.Dispose();
				capture?.Dispose();
			}
		}

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

		private async Task<DxgiTexture> MapTexture(DxgiScreenCapture capture)
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

			var result = new DxgiTexture(data.DataPointer, data.RowPitch, data.SlicePitch);

			result.Add(
				new CompositeDisposable(
					Disposable.Create(() => device.ImmediateContext.UnmapSubresource(targetTexture, 0)),
					targetTexture));

			return result;
		}

		#region IDisposable, ICancelable
		public bool IsDisposed => _disposable.IsDisposed;
		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		public void Dispose()
		{
			_disposable.Dispose();
		}

		public void Add([NotNull] IDisposable disposable)
		{
			_disposable.Add(disposable);
		}

		public void Remove([NotNull] IDisposable disposable)
		{
			_disposable.Remove(disposable);
		}
		#endregion
	}
}