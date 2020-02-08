using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Emphasis.ScreenCapture.Windows.Dxgi
{
	public class DxgiScreenCaptureFactory : IScreenCaptureMethodFactory
	{
		public const string Code = "dxgi";

		public IEnumerable<ScreenCaptureMethod> Create()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
				yield break;

			var method = new ScreenCaptureMethod(Code);
			yield return method;
		}
	}
}
