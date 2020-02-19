using System;
using System.Diagnostics.CodeAnalysis;

namespace Emphasis.ScreenCapture
{
	public class ScreenCapture
	{
		public Screen Screen { get; }
		public DateTime Time { get; }
		public int Width { get; }
		public int Height { get; }

		public IScreenCaptureMethod Method { get; }

		public ScreenCapture(
			[NotNull] Screen screen,
			DateTime time,
			int width,
			int height,
			[NotNull] IScreenCaptureMethod method)
		{
			Screen = screen;
			Time = time;
			Width = width;
			Height = height;
			Method = method;
		}
	}
}
