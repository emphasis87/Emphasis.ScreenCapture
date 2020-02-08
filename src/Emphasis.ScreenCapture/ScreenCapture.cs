using System;
using System.Collections.Generic;
using System.Linq;

namespace Emphasis.ScreenCapture
{
	public class ScreenCapture
	{
		private readonly object _sync = new object();
		private ScreenCaptureMethod[] _methods = new ScreenCaptureMethod[0];

		public ScreenCaptureInfo Capture(ScreenCaptureSettings settings = null)
		{

		}

		public void AddMethodFactory(IScreenCaptureMethodFactory methodFactory)
		{
			lock (_sync)
			{
				var methods = methodFactory.Create()?.ToArray();
				if (methods != null && methods.Length > 0)
					_methods = _methods.Concat(methods).ToArray();
			}
		}
	}
}
