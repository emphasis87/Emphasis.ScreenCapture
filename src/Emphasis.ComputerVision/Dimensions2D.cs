using System;
using System.Collections.Generic;
using System.Text;

namespace Emphasis.ComputerVision
{
	public ref struct Dimensions2D
	{
		public int X;
		public int Y;
		public int N;
		public int Size;
		public int Channels;

		public Dimensions2D(int x, int y, int channels = 1)
		{
			X = x;
			Y = y;
			Channels = channels;
			N = y * x;
			Size = N * Channels;
		}
	}
}
