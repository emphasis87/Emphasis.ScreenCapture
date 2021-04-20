using System;

namespace Emphasis.ScreenCapture
{
	public class ScreenCaptureException : Exception
	{
		public ScreenCaptureException()
		{
		}

		public ScreenCaptureException(string message)
			: base(message)
		{
		}

		public ScreenCaptureException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
