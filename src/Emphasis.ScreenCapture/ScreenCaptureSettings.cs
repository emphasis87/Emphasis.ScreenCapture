using SharpDX.DXGI;

namespace Emphasis.ScreenCapture
{
	public class ScreenCaptureSettings
	{
		public int AdapterId { get; set; }
		public int OutputId { get; set; }

		public Adapter1 Adapter { get; set; }
		public Output Output { get; set; }
	}
}