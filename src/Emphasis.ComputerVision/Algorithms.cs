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

		public static void Sobel(int width, int height, byte[] source, float[] gradient, float[] angle, byte[] neighbors)
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

					for (var c = 0; c < 4; c++)
					{
						var cdx =
							source[(i - 1) * (width * 4) + (j - 1) * 4 + c] * SobelDxMask[0, 0] +
							source[(i - 1) * (width * 4) + (j + 0) * 4 + c] * SobelDxMask[0, 1] +
							source[(i - 1) * (width * 4) + (j + 1) * 4 + c] * SobelDxMask[0, 2] +
							source[(i + 0) * (width * 4) + (j - 1) * 4 + c] * SobelDxMask[1, 0] +
							source[(i + 0) * (width * 4) + (j + 0) * 4 + c] * SobelDxMask[1, 1] +
							source[(i + 0) * (width * 4) + (j + 1) * 4 + c] * SobelDxMask[1, 2] +
							source[(i + 1) * (width * 4) + (j - 1) * 4 + c] * SobelDxMask[2, 0] +
							source[(i + 1) * (width * 4) + (j + 0) * 4 + c] * SobelDxMask[2, 1] +
							source[(i + 1) * (width * 4) + (j + 1) * 4 + c] * SobelDxMask[2, 2];

						var cdy =
							source[(i - 1) * (width * 4) + (j - 1) * 4 + c] * SobelDyMask[0, 0] +
							source[(i - 1) * (width * 4) + (j + 0) * 4 + c] * SobelDyMask[0, 1] +
							source[(i - 1) * (width * 4) + (j + 1) * 4 + c] * SobelDyMask[0, 2] +
							source[(i + 0) * (width * 4) + (j - 1) * 4 + c] * SobelDyMask[1, 0] +
							source[(i + 0) * (width * 4) + (j + 0) * 4 + c] * SobelDyMask[1, 1] +
							source[(i + 0) * (width * 4) + (j + 1) * 4 + c] * SobelDyMask[1, 2] +
							source[(i + 1) * (width * 4) + (j - 1) * 4 + c] * SobelDyMask[2, 0] +
							source[(i + 1) * (width * 4) + (j + 0) * 4 + c] * SobelDyMask[2, 1] +
							source[(i + 1) * (width * 4) + (j + 1) * 4 + c] * SobelDyMask[2, 2];

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
					var g = Gradient(dx, dy);
					gradient[d] = g;

					var a = GradientAngle(dx, dy);
					angle[d] = a;

					
				}
			}
		}

		public static float GradientAngle(float dx, float dy)
		{
			var a = MathF.Atan2(dy, -dx) / MathF.PI + 2.0f;
			if (a >= 2)
				a -= 2;
			a *= 180;
			return a;
		}

		public static float Gradient(float dx, float dy)
		{
			return MathF.Sqrt(dx * dx + dy * dy);
		}

		public static (byte direction, byte count, byte weight0, byte weight1, byte weight2) GradientNeighbors(float angle)
		{
			// Shift the angle by half of a step
			var a = angle + 11.25f; // 360/32
			if (a >= 360)
				a -= 360;

			var b = Convert.ToByte(MathF.Ceiling(a / 22.5f));
			var c = b % 2;
			var count = (byte)(c + 2);

			var direction = Convert.ToByte(MathF.Ceiling(a / 45.0f));
			if (c == 1)
				return (direction, count, 25, 50, 25);

			return (direction, count, 50, 50, 0);
		}

		private static readonly int[,] Neighbors =
		{
			// x   y
			{ +1,  0 }, //   0° E
			{ +1, -1 }, //  45° NE
			{  0, -1 }, //  90° N
			{ -1, -1 }, // 135° NW
			{ -1,  0 }, // 180° W
			{ -1, +1 }, // 225° SW
			{  0, +1 }, // 270° S
			{  1, +1 }, // 315° SE
		};

		public static void NonMaximumSuppression(
			int width, 
			int height, 
			float[] gradient,
			float[] angle,
			byte[] neighbors, 
			float[] destination, 
			float[] cmp1, 
			float[] cmp2)
		{
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
						continue;

					var d = y * width + x;
					destination[d] = 0;

					var g = gradient[d];
					if (g < 30)
						continue;


					var dir = neighbors[d];
					var dx1 = Neighbors[dir, 0];
					var dy1 = Neighbors[dir, 1];
					var dx2 = -dx1;
					var dy2 = -dy1;

					var d1 = (y + dy1) * width + (x + dx1);
					var d2 = (y + dy2) * width + (x + dx2);

					var g1 = gradient[d1];
					var g2 = gradient[d2];

					// Interpolation


					cmp1[d] = g1;
					cmp2[d] = g2;

					if (g >= g1 && g >= g2)
						destination[d] = g;
				}
			}
		}

		public static void StrokeWidthTransform(
			int width,
			int height,
			float[] edges,
			float[] angle,
			float[] swt)
		{

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

		public static byte ConvertAtan2PiAngleTo8Way(float angle)
		{
			var d = (((angle + 2) % 2 + 0.125f) * 4 - 0.5f) % 7;
			return Convert.ToByte(MathF.Round(d));
		}
	}
}
