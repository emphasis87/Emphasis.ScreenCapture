using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Emphasis.ComputerVision
{
	public unsafe struct Component
	{
		public int Color;
		public int Count;
		public int SwtCount;
		public int SwtSum;
		public int X0;
		public int X1;
		public int Y0;
		public int Y1;
		public int Width;
		public int Height;
		public float SwtVariance;
		public float SwtAverage;
		public int SwtMedian;
		public fixed int Channel[4];
	}
}
