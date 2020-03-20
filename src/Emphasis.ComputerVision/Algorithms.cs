using System;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Emphasis.ComputerVision
{
	public static partial class Algorithms
	{
		private static readonly float[] GrayscaleMask = 
		{ 
			0.2126f, // R
			0.7152f, // G
			0.0722f, // B
			0
		};

		public static void Grayscale(int width, int height, byte[] source, byte[] grayscale)
		{
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var g =
						source[y * (width * 4) + x * 4 + 0] * GrayscaleMask[0] +
						source[y * (width * 4) + x * 4 + 1] * GrayscaleMask[1] +
						source[y * (width * 4) + x * 4 + 2] * GrayscaleMask[2] +
						source[y * (width * 4) + x * 4 + 3] * GrayscaleMask[3];

					var d = y * width + x;
					grayscale[d] = Convert.ToByte(Math.Min(g, 255));
				}
			}
		}

		//constant float gauss[3][3] = 
		//{   
		//	{ 0.0625, 0.1250, 0.0625 },
		//	{ 0.1250, 0.2500, 0.1250 },
		//	{ 0.0625, 0.1250, 0.0625 },
		//};
		
		public static void Gauss(int width, int height, byte[] source, byte[] destination)
		{
			for (var y = 0; y < height; y++)
			{
				var i = Clamp(y, height);
				for (var x = 0; x < width; x++)
				{
					var j = Clamp(x, width);
					for (var channel = 0; channel < 4; channel++)
					{
						var result =
							source[(i - 1) * (width * 4) + (j - 1) * 4 + channel] * 0.0625f +
							source[(i - 1) * (width * 4) + (j + 0) * 4 + channel] * 0.1250f +
							source[(i - 1) * (width * 4) + (j + 1) * 4 + channel] * 0.0625f +
							source[(i + 0) * (width * 4) + (j - 1) * 4 + channel] * 0.1250f +
							source[(i + 0) * (width * 4) + (j + 0) * 4 + channel] * 0.2500f +
							source[(i + 0) * (width * 4) + (j + 1) * 4 + channel] * 0.1250f +
							source[(i + 1) * (width * 4) + (j - 1) * 4 + channel] * 0.0625f +
							source[(i + 1) * (width * 4) + (j + 0) * 4 + channel] * 0.1250f +
			 				source[(i + 1) * (width * 4) + (j + 1) * 4 + channel] * 0.0625f;

						var d = y * (width * 4) + x * 4 + channel;
						destination[d] = Convert.ToByte(Math.Min(255, result));
					}
				}
			}
		}

		private static readonly float[,] SobelDxMask = new float[,]
		{
			{ +1, +0, -1 },
			{ +2, +0, -2 },
			{ +1, +0, -1 },
		};

		private static readonly float[,] SobelDyMask = new float[,]
		{
			{ +1, +2, +1 },
			{  0,  0,  0 },
			{ -1, -2, -1 },
		};

		public static void Sobel(int width, int height, byte[] source, float[] gradient, byte[] direction)
		{
			for (var y = 0; y < height; y++)
			{
				var i = Clamp(y, height);
				for (var x = 0; x < width; x++)
				{
					var j = Clamp(x, width);

					var dx = 0.0f;
					var dxAbs = 0.0f;
					var dy = 0.0f;
					var dyAbs = 0.0f;

					for (var channel = 0; channel < 4; channel++)
					{
						var cdx =
							source[(i - 1) * (width * 4) + (j - 1) * 4 + channel] * SobelDxMask[0, 0] +
							source[(i - 1) * (width * 4) + (j + 0) * 4 + channel] * SobelDxMask[0, 1] +
							source[(i - 1) * (width * 4) + (j + 1) * 4 + channel] * SobelDxMask[0, 2] +
							source[(i + 0) * (width * 4) + (j - 1) * 4 + channel] * SobelDxMask[1, 0] +
							source[(i + 0) * (width * 4) + (j + 0) * 4 + channel] * SobelDxMask[1, 1] +
							source[(i + 0) * (width * 4) + (j + 1) * 4 + channel] * SobelDxMask[1, 2] +
							source[(i + 1) * (width * 4) + (j - 1) * 4 + channel] * SobelDxMask[2, 0] +
							source[(i + 1) * (width * 4) + (j + 0) * 4 + channel] * SobelDxMask[2, 1] +
							source[(i + 1) * (width * 4) + (j + 1) * 4 + channel] * SobelDxMask[2, 2];

						var cdy =
							source[(i - 1) * (width * 4) + (j - 1) * 4 + channel] * SobelDyMask[0, 0] +
							source[(i - 1) * (width * 4) + (j + 0) * 4 + channel] * SobelDyMask[0, 1] +
							source[(i - 1) * (width * 4) + (j + 1) * 4 + channel] * SobelDyMask[0, 2] +
							source[(i + 0) * (width * 4) + (j - 1) * 4 + channel] * SobelDyMask[1, 0] +
							source[(i + 0) * (width * 4) + (j + 0) * 4 + channel] * SobelDyMask[1, 1] +
							source[(i + 0) * (width * 4) + (j + 1) * 4 + channel] * SobelDyMask[1, 2] +
							source[(i + 1) * (width * 4) + (j - 1) * 4 + channel] * SobelDyMask[2, 0] +
							source[(i + 1) * (width * 4) + (j + 0) * 4 + channel] * SobelDyMask[2, 1] +
							source[(i + 1) * (width * 4) + (j + 1) * 4 + channel] * SobelDyMask[2, 2];

						var cdxAbs = MathF.Abs(cdx);
						if (cdxAbs > dxAbs)
						{
							dx = cdx;
							dxAbs = cdxAbs;
						}

						var cdyAbs = MathF.Abs(cdy);
						if (cdyAbs > dyAbs)
						{
							dy = cdy;
							dyAbs = cdyAbs;
						}
					}

					var d = y * width + x;

					// Up to sqrt(255*4 * 255*4 + 255*4 * 255*4) = 1442
					var g = MathF.Sqrt(dx * dx + dy * dy);
					gradient[d] = g;

					var a = MathF.Atan2(dy, dx) / MathF.PI;
					// Convert the angle into 8 distinct directions
					var dirf = (a + 1.125f) * 4 - 1;
					var dir = Convert.ToByte(MathF.Round(dirf));
					// Indexes 0,8,9 denote the same direction
					if (dir > 7)
						dir = 0;

					direction[d] = dir;
				}
			}
		}

		private static readonly int[,] Neighbors =
		{
			// x   y
			{ -1,  0 }, // W
			{ -1, -1 }, // NW
			{  0, -1 }, // N
			{  1, -1 }, // NE
			{  1,  0 }, // E
			{  1,  1 }, // SE
			{  0,  1 }, // S
			{  1,  1 }, // SW
		};

		public static void NonMaximumSuppression(int width, int height, float[] gradient, byte[] direction, float[] destination)
		{
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					var g = gradient[d];
					if (g < 30)
						continue;

					var dir = direction[d];
					var dx1 = Neighbors[dir, 0];
					var dy1 = Neighbors[dir, 1];
					var dx2 = -dx1;
					var dy2 = -dy1;

					var x1 = x;
					var x2 = x;
					var y1 = y;
					var y2 = y;

					var m1 = true;
					var m2 = true;

					bool CompareGradient(ref bool m, ref int xn, ref int yn, int dx, int dy)
					{
						xn += dx;
						yn += dy;

						if (xn < 0 || xn > width - 1 || yn < 0 || yn > height - 1)
						{
							m = false;
							return false;
						}

						var dn = yn * width + xn;
						var gn = gradient[dn];
						if (gn < 30)
						{
							m = false;
							return false;
						}

						if (gn <= g)
							return false;

						// Suppress this edge because a larger gradient found
						g = 0;
						return true;
					}

					// Move in parallel width the edge in both directions
					while (m1 || m2)
					{
						if (m1 && CompareGradient(ref m1, ref x1, ref y1, dx1, dy1))
							break;
							
						if (m2 && CompareGradient(ref m2, ref x2, ref y2, dx2, dy2))
							break;
					}

					destination[d] = g;
				}
			}
		}

		public static void Normalize(this float[] source, byte[] destination, int channels)
		{
			var min = new float[channels];
			var max = new float[channels];

			for (var c = 0; c < channels; c++)
			{
				min[c] = float.MaxValue;
				max[c] = float.MinValue;
			}

			for (var i = 0; i < source.Length; i++)
			{
				for (var c = 0; c < channels; c++)
				{
					var v = source[i + c];
					if (v < min[c])
						min[c] = v;
					if (v > max[c])
						max[c] = v;
				}
			}

			var len = new float[channels];
			for (var c = 0; c < channels; c++)
			{
				len[c] = max[c] - min[c];
			}

			for (var i = 0; i < source.Length; i++)
			{
				for (var c = 0; c < channels; c++)
				{
					var a = min[c];
					var l = len[c];
					var v = source[i + c];
					var r = ((v - a) / l) * 255;
					destination[i + c] = Convert.ToByte(r);
				}
			}
		}

		private static int Clamp(int value, int max)
		{
			if (value == 0)
				return 1;
			if (value == max - 1)
				return max - 2;
			return value;
		}

	}
}
