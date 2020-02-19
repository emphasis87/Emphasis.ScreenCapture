using System.Collections.Generic;
using System.Runtime.InteropServices;
using Emphasis.ScreenCapture.Windows.Dxgi;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureMethodSelector
	{
		IEnumerable<IScreenCaptureMethod> GetMethods();
	}

	public class ScreenCaptureMethodSelector : IScreenCaptureMethodSelector
	{
		public IEnumerable<IScreenCaptureMethod> GetMethods()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				yield return new DxgiScreenCaptureMethod();
			}
		}
	}
}