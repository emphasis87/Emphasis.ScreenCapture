using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Emphasis.ScreenCapture.Windows.Gdi
{
	public class GdiScreenCaptureFactory : IScreenCaptureMethodFactory
	{
		public const string Code = "gdi";

		public IEnumerable<ScreenCaptureMethod> Create()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				yield break;

			var method = new ScreenCaptureMethod(Code);
			yield return method;
		}
	}
}