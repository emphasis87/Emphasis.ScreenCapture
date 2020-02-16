using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public class DxgiScreenCapture : ScreenCapture
	{
		public Resource ScreenResource { get; }

		public DxgiScreenCapture(Adapter1 adapter, Output1 output, int width, int height) : base(adapter, output, width, height)
		{
		}

		public override byte[] GetBytes()
		{
			return null;
		}

		public override void Dispose()
		{
			ScreenResource.Dispose();
		}
	}
}