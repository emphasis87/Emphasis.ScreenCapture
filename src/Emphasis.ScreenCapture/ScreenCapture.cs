using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture
{
	public abstract class ScreenCapture : IDisposable
	{
		public Adapter1 Adapter { get; }
		public Output1 Output { get; }

		public int Width { get; }
		public int Height { get; }

		protected ScreenCapture(Adapter1 adapter, Output1 output, int width, int height)
		{
			Adapter = adapter;
			Output = output;
			Width = width;
			Height = height;
		}

		public abstract byte[] GetBytes();
		public abstract void Dispose();
	}
}
