using System;
using System.Diagnostics.CodeAnalysis;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiScreenCapture : ScreenCapture
	{
		public Adapter1 Adapter { get; }
		public Output1 Output { get; }
		public Device Device { get; }
		public OutputDuplication OutputDuplication { get; }
		public Resource ScreenResource { get; }
		public OutputDuplicateFrameInformation FrameInformation { get; }

		public DxgiScreenCapture(
			[NotNull] IScreen screen,
			[NotNull] IScreenCaptureMethod method,
			DateTime time,
			int width,
			int height,
			[NotNull] Adapter1 adapter,
			[NotNull] Output1 output,
			[NotNull] Device device,
			[NotNull] OutputDuplication outputDuplication,
			[NotNull] Resource screenResource,
			[NotNull] OutputDuplicateFrameInformation frameInformation)
			: base(screen, method, time, width, height)
		{
			Adapter = adapter;
			Output = output;
			Device = device;
			OutputDuplication = outputDuplication;
			ScreenResource = screenResource;
			FrameInformation = frameInformation;
		}
	}
}