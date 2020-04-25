using System;
using System.Collections.Generic;
using System.Text;
using RBush;

namespace Emphasis.ComputerVision
{
	public class Point2D : ISpatialData
	{
		public int X { get; }
		public int Y { get; }
		public int Data { get; }

		private readonly Envelope _envelope;
		public ref readonly Envelope Envelope => ref _envelope;

		public Point2D(int x, int y, int data = 0)
		{
			X = x;
			Y = y;
			Data = data;
			_envelope = new Envelope(x, y, x, y);
		}
	}
}
