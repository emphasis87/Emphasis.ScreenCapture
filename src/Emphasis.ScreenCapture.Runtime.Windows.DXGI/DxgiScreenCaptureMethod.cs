using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public interface IDxgiScreenCaptureMethod : IScreenCaptureMethod, IScreenProvider
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
	}
}