using System;
using System.Collections.Generic;
using System.Text;

namespace Emphasis.ComputerVision
{
	public class Matrix<T>
	{
		public int Width;
		public int Height;
		public int Channels;
		public Memory<T> Data;

		public Matrix(int width, int height, int channels, Memory<T> data)
		{
			Width = width;
			Height = height;
			Channels = channels;
			Data = data;
		}
	}
}
