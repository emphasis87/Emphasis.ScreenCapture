using System;
using System.Diagnostics.CodeAnalysis;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public class DxgiScreenCapture : ScreenCapture
	{
		public Adapter1 Adapter { get; }
		public Output1 Output { get; }
		public OutputDuplication OutputDuplication { get; }
		public Resource ScreenResource { get; }
		public OutputDuplicateFrameInformation FrameInformation { get; }

		public DxgiScreenCapture(
			[NotNull] Screen screen,
			DateTime time,
			int width,
			int height,
			[NotNull] IScreenCaptureMethod method,
			[NotNull] Adapter1 adapter,
			[NotNull] Output1 output,
			[NotNull] OutputDuplication outputDuplication,
			[NotNull] Resource screenResource,
			[NotNull] OutputDuplicateFrameInformation frameInformation)
			: base(screen, time, width, height, method)
		{
			Adapter = adapter;
			Output = output;
			OutputDuplication = outputDuplication;
			ScreenResource = screenResource;
			FrameInformation = frameInformation;
		}
	}
}