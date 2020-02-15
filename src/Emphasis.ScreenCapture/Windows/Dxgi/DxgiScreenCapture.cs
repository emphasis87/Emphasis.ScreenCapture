using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public class DxgiScreenCapture : ScreenCapture
	{
		public Resource ScreenResource { get; }

		public DxgiScreenCapture(Resource screenResource)
		{
			ScreenResource = screenResource;
		}

		public override void Dispose()
		{
			ScreenResource.Dispose();
		}
	}
}