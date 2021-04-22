using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Rectangle = SharpDX.Rectangle;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public interface IDxgiScreenCaptureMethod : IScreenCaptureMethod
	{

	}

	public class DxgiScreenCaptureMethod : IDxgiScreenCaptureMethod
	{
		private readonly IScreenCaptureModule _module;

		private readonly ConcurrentDictionary<IScreen, Lazy<DxgiScreenCaptureSharedResources>> _sharedResources =
			new ConcurrentDictionary<IScreen, Lazy<DxgiScreenCaptureSharedResources>>();

		public DxgiScreenCaptureMethod(IScreenCaptureModule module)
		{
			_module = module;
		}

		public async Task<IScreenCapture> Capture(IScreen screen, CancellationToken cancellationToken = default)
		{
			var stream = CaptureStream(screen, cancellationToken);
			return await stream.FirstOrDefaultAsync(cancellationToken);
		}

		private DxgiScreenCaptureSharedResources GetSharedResources(IScreen screen)
		{
			var provider = _sharedResources.GetOrAdd(screen, new Lazy<DxgiScreenCaptureSharedResources>(() => CreateSharedResources(screen)));
			return provider.Value;
		}

		private DxgiScreenCaptureSharedResources CreateSharedResources(IScreen screen)
		{
			var factory = new Factory1();
			var adapter = factory.Adapters1.FirstOrDefault(x => x.Description.DeviceId == screen.AdapterId);
			if (adapter == null)
				throw new ArgumentOutOfRangeException(nameof(screen), screen.AdapterId, $"Unable to find an adapter width the id: {screen.AdapterId}.");

			var output = adapter.Outputs.FirstOrDefault(x => x.Description.DeviceName == screen.OutputName);
			if (output == null)
				throw new ArgumentOutOfRangeException(nameof(screen), screen.OutputName, $"Unable to find an output with the name: {screen.OutputName}.");

			var device = new Device(adapter);
			var output1 = output.QueryInterface<Output1>();
			var outputDuplication = output1.DuplicateOutput(device);

			var sharedResources = new DxgiScreenCaptureSharedResources(factory, adapter, output, device, output1, outputDuplication);
			return sharedResources;
		}

		public async IAsyncEnumerable<IScreenCapture> CaptureStream(IScreen screen, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var sharedResources = GetSharedResources(screen);

			var bounds = (Rectangle)sharedResources.Output.Description.DesktopBounds;
			var width = bounds.Width;
			var height = bounds.Height;

			var outputDuplication = sharedResources.OutputDuplication;

			try
			{
				sharedResources.Acquire();
				while (!cancellationToken.IsCancellationRequested)
				{
					// Previous frame must be released prior to acquiring the next frame
					if (sharedResources.IsFrameAcquired)
						throw new ScreenCaptureException($"Previous {typeof(DxgiScreenCapture)} must be disposed prior to acquiring the next frame.");
					
					// Acquire the next frame
					var (result, frameInformation, screenResource) = await Task.Run(() =>
						{
							var err = outputDuplication.TryAcquireNextFrame(1000, out var fi, out var sr);
							return (err, fi, sr);
						},
						cancellationToken);

					if (result != Result.Ok)
					{
						screenResource?.Dispose();
						throw new ScreenCaptureException($"{typeof(OutputDuplication)}.{nameof(OutputDuplication.TryAcquireNextFrame)} returned error: {result}.");
					}

					if (frameInformation.AccumulatedFrames == 0)
					{
						outputDuplication.ReleaseFrame();
						continue;
					}

					var capture = new DxgiScreenCapture(screen, _module, DateTime.Now, width, height,
						sharedResources, screenResource, frameInformation);

					yield return capture;
				}
			}
			finally
			{
				sharedResources.Release();
			}
		}
	}
}