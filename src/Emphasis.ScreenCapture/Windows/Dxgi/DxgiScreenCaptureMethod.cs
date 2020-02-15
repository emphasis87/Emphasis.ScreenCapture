using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpDX;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public class DxgiScreenCaptureMethod : IDisposable
	{
		private readonly Lazy<Factory1> _factory1 = 
			new Lazy<Factory1>(() => new Factory1());

		protected Factory1 Factory1 => _factory1.Value;

		public DxgiScreenCaptureMethod()
		{
		}

		public async IAsyncEnumerable<ScreenCapture> Capture(
			ScreenCaptureSettings settings, 
			[EnumeratorCancellation]CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			var adapterId = settings.AdapterId;
			var outputId = settings.OutputId;

			using var adapter = Factory1.GetAdapter1(adapterId);
			using var device = new Device(adapter);
			using var output = adapter.GetOutput(outputId);
			using var output1 = output.QueryInterface<Output1>();
			using var duplicatedOutput = output1.DuplicateOutput(device);

			var bounds = (SharpDX.Rectangle) output.Description.DesktopBounds;
			var width = bounds.Width;
			var height = bounds.Height;

			while (!cancellationToken.IsCancellationRequested)
			{
				Resource screenResource;
				OutputDuplicateFrameInformation frameInformation;

				var result = duplicatedOutput.TryAcquireNextFrame(1000, out frameInformation, out screenResource);
				if (result != Result.Ok)
					yield break;

				yield return new DxgiScreenCapture(screenResource);
			}
		}

		public void Dispose()
		{
			if (_factory1.IsValueCreated)
				_factory1.Value.Dispose();
		}
	}
}