using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public class DxgiScreenCaptureMethod : IScreenCaptureMethod
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
				if (result != Result.Ok)
					yield break;

				var capture = new DxgiScreenCapture(screen, DateTime.Now, width, height, this, adapter, output1, outputDuplication, screenResource, frameInformation);
				yield return capture;

				outputDuplication.ReleaseFrame();
			}
		}
	}
}