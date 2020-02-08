using System.Collections.Generic;

namespace Emphasis.ScreenCapture.Windows.Gdi
{
	public class GdiScreenCaptureFactory : IScreenCaptureMethodFactory
	{
		public const string Code = "gdi";

		public IEnumerable<ScreenCaptureMethod> Create()
		{
			var method = new ScreenCaptureMethod(Code);

			yield return method;
		}
	}
}