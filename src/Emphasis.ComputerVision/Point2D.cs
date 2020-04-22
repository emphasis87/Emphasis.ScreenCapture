using System;
using System.Collections.Generic;
using System.Text;
using RBush;

namespace Emphasis.ComputerVision
{
	public class Point2D : ISpatialData
	{
		public int X0 { get; }
		public int X1 { get; }
		public int Y0 { get; }
		public int Y1 { get; }
		public int Color { get; }

		private readonly Envelope _envelope;
		public ref readonly Envelope Envelope => ref _envelope;

		public Point2D(int x0, int x1, int y0, int y1, int color)
		{
			X0 = x0;
			X1 = x1;
			Y0 = y0;
			Y1 = y1;
			Color = color;
			_envelope = new Envelope(x0, y0, x0, y0);
		}
	}
}
