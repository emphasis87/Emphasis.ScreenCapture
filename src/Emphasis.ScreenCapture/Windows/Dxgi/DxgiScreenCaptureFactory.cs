using System;
using System.Collections.Generic;
using System.Text;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public class DxgiScreenCaptureFactory : IScreenCaptureMethodFactory
	{
		public const string Code = "dxgi";

		public IEnumerable<ScreenCaptureMethod> Create()
		{
			var method = new ScreenCaptureMethod(Code);

			yield return method;
		}
	}
}
