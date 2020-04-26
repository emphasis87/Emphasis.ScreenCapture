using System;

namespace Emphasis.ComputerVision
{
	public unsafe struct Component
	{
		public int Color;
		public int Validity;
		public int Size;
		public int SwtSize;
		public int SwtSum;
		public int X0;
		public int X1;
		public int Y0;
		public int Y1;
		public int Width;
		public int Height;
		public int MinDimension;
		public int MaxDimension;
		public float SizeRatio;
		public float Diameter;
		public float DiameterToSwtMedianRatio;
		public float SwtVariance;
		public float SwtAverage;
		public int SwtMedian;
		public fixed int ChannelSum[4];
		public fixed int ChannelAverage[4];

		public void Initialize()
		{
			X0 = int.MaxValue;
			X1 = int.MinValue;
			Y0 = int.MaxValue;
			Y1 = int.MinValue;
		}

		public bool IsValid()
		{
			return Validity == 0;
		}

		public Point2D LeftTop(int data = 0) => new Point2D(X0, Y0, data);
		public Box2D BoundingBox(int data = 0) => new Box2D(X0, X1, Y0, Y1, data);

		public const int HasLowVariance = 1;
		public const int IsSizeProportional = 1 << 1;
		public const int IsDiameterSmall = 1 << 2;
	}
}
