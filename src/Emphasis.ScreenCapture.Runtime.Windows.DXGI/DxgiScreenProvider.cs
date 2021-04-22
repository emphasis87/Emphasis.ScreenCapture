using System.Linq;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public interface IDxgiScreenProvider : IScreenProvider
	{

	}

	public class DxgiScreenProvider : IDxgiScreenProvider
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
	}
}
