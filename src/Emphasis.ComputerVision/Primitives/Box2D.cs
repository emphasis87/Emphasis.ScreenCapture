using System;
using RBush;

namespace Emphasis.ComputerVision.Primitives
{
	public class Box2D : ISpatialData
	{
		public int X0 { get; }
		public int X1 { get; }
		public int Y0 { get; }
		public int Y1 { get; }
		public int Width { get; }
		public int Height { get; }
		public int Data { get; }

		private readonly Envelope _envelope;
		public ref readonly Envelope Envelope => ref _envelope;

		public Box2D(int x0, int x1, int y0, int y1, int data = 0)
		{
			X0 = x0;
			X1 = x1;
			Y0 = y0;
			Y1 = y1;
			Width = x1 - x0;
			Height = y1 - y0;
			Data = data;
			_envelope = new Envelope(x0, y0, x1, y1);
		}

		public bool Contains(Box2D other)
		{
			if (other.Width > Width || other.Height > Height)
				return false;

			var dx = Math.Min(X1, other.X1) - Math.Max(X0, other.X0);
			if (dx < other.Width)
				return false;

			var dy = Math.Min(Y1, other.Y1) - Math.Max(Y0, other.Y0);
			return dy >= other.Height;
		}
	}
}
