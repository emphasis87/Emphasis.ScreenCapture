using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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
		
		public static void Gauss(int width, int height, byte[] source, byte[] destination, int channels = 4)
		{
			for (var y = 0; y < height; y++)
			{
				var i = Clamp(y, height);
				for (var x = 0; x < width; x++)
				{
					var j = Clamp(x, width);
					for (var c = 0; c < channels; c++)
					{
						var result =
							source[(i - 1) * (width * channels) + (j - 1) * channels + c] * 0.0625f +
							source[(i - 1) * (width * channels) + (j + 0) * channels + c] * 0.1250f +
							source[(i - 1) * (width * channels) + (j + 1) * channels + c] * 0.0625f +
							source[(i + 0) * (width * channels) + (j - 1) * channels + c] * 0.1250f +
							source[(i + 0) * (width * channels) + (j + 0) * channels + c] * 0.2500f +
							source[(i + 0) * (width * channels) + (j + 1) * channels + c] * 0.1250f +
							source[(i + 1) * (width * channels) + (j - 1) * channels + c] * 0.0625f +
							source[(i + 1) * (width * channels) + (j + 0) * channels + c] * 0.1250f +
			 				source[(i + 1) * (width * channels) + (j + 1) * channels + c] * 0.0625f;

						var d = y * (width * channels) + x * channels + c;
						destination[d] = Convert.ToByte(Math.Min(255, result));
					}
				}
			}
		}

		private static readonly float[,] SobelDxMask3 = new float[,]
		{
			{ +1, +0, -1 },
			{ +2, +0, -2 },
			{ +1, +0, -1 },
		};

		private static readonly float[,] SobelDyMask3 = new float[,]
		{
			{ +1, +2, +1 },
			{  0,  0,  0 },
			{ -1, -2, -1 },
		};

		private static readonly float[,] SobelDxMask5 = new float[,]
		{
			{ +1, +2, +0, -2, -1 },
			{ +4, +8, +0, -8, -4 },
			{ +6,+12, +0,-12, -6 },
			{ +4, +8, +0, -8, -4 },
			{ +1, +2, +0, -2, -1 },
		};

		private static readonly float[,] SobelDyMask5 = new float[,]
		{
			{ +1, +4, +6, +2, +1 },
			{ +2, +8,+12, +8, +4 },
			{ +0, +0, +0, +0, +0 },
			{ -2, -8,-12, -8, -4 },
			{ -1, -4, -6, -4, -1 },
		};

		//private static readonly float[,] SobelDxMask3 = new float[,]
		//{
		//	{  +3, +0,  -3 },
		//	{ +10, +0, -10 },
		//	{  +3, +0,  -3 },
		//};

		//private static readonly float[,] SobelDyMask3 = new float[,]
		//{
		//	{ +3, +10, +3 },
		//	{  0,  0,  0 },
		//	{ -3, -10, -10 },
		//};

		public static void Sobel3(int width, int height, byte[] source, float[] dx, float[] dy, float[] gradient, float[] angle, byte[] neighbors, int channels = 4)
		{
			for (var y = 0; y < height; y++)
			{
				var i = Clamp(y, height);
				for (var x = 0; x < width; x++)
				{
					var j = Clamp(x, width);

					var idx = 0.0f;
					var idxa = 0.0f;
					var idy = 0.0f;
					var idya = 0.0f;
					var cdx = 0.0f;
					var cdy = 0.0f;

					for (var c = 0; c < channels; c++)
					{
						for (var iy = 0; iy < 3; iy++)
						{
							for (var ix = 0; ix < 3; ix++)
							{
								cdx += source[(i + (iy - 1)) * (width * channels) + (j + (ix - 1)) * channels + c] * SobelDxMask3[iy, ix];
								cdy += source[(i + (iy - 1)) * (width * channels) + (j + (ix - 1)) * channels + c] * SobelDyMask3[iy, ix];
							}
						}

						cdx /= 8;
						cdy /= 8;

						var cdxAbs = MathF.Abs(cdx);
						if (cdxAbs > idxa)
						{
							idx = cdx;
							idxa = cdxAbs;
						}

						var cdyAbs = MathF.Abs(cdy);
						if (cdyAbs > idya)
						{
							idy = cdy;
							idya = cdyAbs;
						}
					}

					var d = y * width + x;

					dx[d] = idx;
					dy[d] = idy;

					// Up to sqrt(255*4 * 255*4 + 255*4 * 255*4) = 1442
					var g = Gradient(idx, idy);
					gradient[d] = g;

					var a = GradientAngle(idx, idy);
					angle[d] = a;

					var (direction, count, w0, w1, w2) = GradientNeighbors(a);
					var dn = y * width * 5 + x * 5;
					neighbors[dn] = direction;
					neighbors[dn + 1] = count;
					neighbors[dn + 2] = w0;
					neighbors[dn + 3] = w1;
					neighbors[dn + 4] = w2;
				}
			}
		}

		public static void Sobel5(int width, int height, byte[] source, float[] dx, float[] dy, float[] gradient, float[] angle, byte[] neighbors, int channels = 4)
		{
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var idx = 0.0f;
					var idxa = 0.0f;
					var idy = 0.0f;
					var idya = 0.0f;
					var cdx = 0.0f;
					var cdy = 0.0f;

					for (var c = 0; c < channels; c++)
					{
						for (var iy = 0; iy < 5; iy++)
						{
							for (var ix = 0; ix < 5; ix++)
							{
								var yn = ClampToEdge(y + (iy - 2), height);
								var xn = ClampToEdge(x + (ix - 2), width);
								cdx += source[yn * width * channels + xn * channels + c] * SobelDxMask5[iy, ix];
								cdy += source[yn * width * channels + xn * channels + c] * SobelDyMask5[iy, ix];
							}
						}

						cdx /= 16;
						cdy /= 16;

						var cdxAbs = MathF.Abs(cdx);
						if (cdxAbs > idxa)
						{
							idx = cdx;
							idxa = cdxAbs;
						}

						var cdyAbs = MathF.Abs(cdy);
						if (cdyAbs > idya)
						{
							idy = cdy;
							idya = cdyAbs;
						}
					}

					var d = y * width + x;

					dx[d] = idx;
					dy[d] = idy;

					// Up to sqrt(255*4 * 255*4 + 255*4 * 255*4) = 1442
					var g = Gradient(idx, idy);
					gradient[d] = g;

					var a = GradientAngle(idx, idy);
					angle[d] = a;

					var (direction, count, w0, w1, w2) = GradientNeighbors(a);
					var dn = y * width * 5 + x * 5;
					neighbors[dn] = direction;
					neighbors[dn + 1] = count;
					neighbors[dn + 2] = w0;
					neighbors[dn + 3] = w1;
					neighbors[dn + 4] = w2;
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
			if (direction >= 8)
				direction -= 8;

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
					if (g < 15)
						continue;

					var dn = y * width * 5 + x * 5;
					var neighbor = neighbors[dn];
					var count = neighbors[dn + 1];

					var g1 = 0f;
					var g2 = 0f;

					for (var i = 0; i < count; i++)
					{
						var weight = neighbors[dn + 2 + i];
						var n = neighbor - i;
						if (n < 0)
							n += 8;

						var dx1 = Neighbors[n, 0];
						var dy1 = Neighbors[n, 1];
						var dx2 = -dx1;
						var dy2 = -dy1;

						var d1 = (y + dy1) * width + (x + dx1);
						var d2 = (y + dy2) * width + (x + dx2);

						g1 += weight * gradient[d1];
						g2 += weight * gradient[d2];
					}

					g1 *= 0.01f;
					g2 *= 0.01f;

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
			byte[] source,
			float[] edges,
			float[] angles,
			float[] dx,
			float[] dy,
			int[] swt0,
			int[] swt1,
			int rayLength = 20)
		{
			// Prefix scan edges
			var edgeList = new List<int>();
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					var g = edges[d];
					if (g > 0)
					{
						edgeList.Add(x);
						edgeList.Add(y);
					}
				}
			}

			// Find the stroke width in positive direction
			var swtList0 = new List<int>();
			StrokeWidthTransform(width, height, source, edges, angles, dx, dy, swt0, rayLength, true, edgeList, swtList0);

			// Find the stroke width in positive direction
			var swtList1 = new List<int>();
			StrokeWidthTransform(width, height, source, edges, angles, dx, dy, swt1, rayLength, false, edgeList, swtList1);
		}

		public static void StrokeWidthTransform(
			int width,
			int height,
			byte[] source,
			float[] edges,
			float[] angles,
			float[] dx,
			float[] dy,
			int[] swt,
			int rayLength,
			bool direction,
			List<int> edgeList,
			List<int> swtList,
			int channels = 4)
		{
			var dir = direction ? 1 : -1;

			int x, y, d, len, cx, cy, mx, my, ix, iy;
			float a, idx, idy, idxa, idya, ex, ey;

			void InitializeLine()
			{
				d = y * width + x;

				// Differential on x and y-axis
				idx = dx[d];
				idy = dy[d];

				// Sign of the differential in the direction (black on white or white on black)
				ix = dir * MathF.Sign(idx);
				iy = dir * MathF.Sign(idy);

				// The size of the differential
				idxa = MathF.Abs(idx);
				idya = MathF.Abs(idy);

				ResetLine();
			}

			void ResetLine()
			{
				// The current error
				ex = idxa;
				ey = idya;

				// The current position
				cx = x;
				cy = y;
			}

			void Advance()
			{
				// Move by 1 (direct neighbor is likely an edge in the same direction)
				if (ex >= ey)
				{
					ey += idya;
					cx += ix;
				}
				else
				{
					ex += idxa;
					cy += iy;
				}
			}

			Span<byte> src = stackalloc byte[channels];

			var en = edgeList.Count;
			for (var i = 0; i < en; i += 2)
			{
				x = edgeList[i];
				y = edgeList[i + 1];

				for (var c = 0; c < channels; c++)
				{
					src[c] = source[y * width * channels + x * channels + c];
				}
				
				InitializeLine();

				a = angles[d];

				// The indexing limits
				mx = ix > 0 ? width : -1;
				my = iy > 0 ? height : -1;

				Advance();

				if (cx == mx || cy == my)
					continue;

				for (var ci = 2; ci < rayLength; ci++)
				{
					Advance();

					if (cx == mx || cy == my)
						break;

					// The current distance
					var cd = cy * width + cx;
					var cg = edges[cd];

					var isSameColor = true;
					// Not an edge
					if (cg <= 0)
					{
						for (var c = 0; c < channels; c++)
						{
							var color = source[cy * width * channels + cx * channels + c];
							if (Math.Abs(src[c] - color) > 50)
							{
								isSameColor = false;
								break;
							}
						}
						
					}

					if (!isSameColor || cg > 0)
					{
						// Check that the found edge is roughly opposite
						var ca = angles[cd];
						var cad = MathF.Abs(180 - MathF.Abs(a - ca));
						if (cad < 45)
						{
							swtList.Add(x);
							swtList.Add(y);
							swtList.Add(ci);
						}
						break;
					}
				}
			}

			/*
			var sn = swtList.Count;
			for (var i = 0; i < sn; i += 3)
			{
				var x = swtList[i];
				var y = swtList[i + 1];
				var len = swtList[i + 2];
				var d = y * width + x;

				swt[d] = 255;
			}
			*/

			// Fill in the strokes
			var sn = swtList.Count;
			for (var i = 0; i < sn; i += 3)
			{
				x = swtList[i];
				y = swtList[i + 1];
				len = swtList[i + 2];

				InitializeLine();

				Advance();

				for (var ci = 1; ci < len; ci++)
				{
					// The current distance
					var cd = cy * width + cx;
					var cs = swt[cd];
					// Set the stroke width to the lowest found
					if (cs > len)
						swt[cd] = len;

					Advance();
				}
			}

			for (int i = 0, j = 0; i < sn; j++, i += 3)
			{
				x = swtList[i];
				y = swtList[i + 1];
				len = swtList[i + 2];
				
				InitializeLine();

				Advance();

				// Find the median stroke width for the ray
				var sm = new List<int>();
				for (var ci = 1; ci < len; ci++)
				{
					// The current distance
					var cd = cy * width + cx;
					if (cy < 0 || cy >= height || cx < 0 || cx >= width)
						throw new ArgumentOutOfRangeException();
					var cs = swt[cd];
					sm.Add(cs);

					Advance();
				}

				var median = Median(sm);

				ResetLine();

				Advance();

				// Cap the stroke width to the ray's median
				for (var ci = 1; ci < len; ci++)
				{
					// The current distance
					var cd = cy * width + cx;
					if (cy < 0 || cy >= height || cx < 0 || cx >= width)
						throw new ArgumentOutOfRangeException();
					var cs = swt[cd];
					if (cs > median)
						swt[cd] = median;

					Advance();
				}
			}
		}

		public static int ColorComponentsWatershed(
			int width,
			int height,
			byte[] source,
			int[] swt,
			int[] components,
			int channels = 1)
		{
			Algorithms.PrepareComponents(swt, components);

			var rounds = 0;
			var isComplete = false;
			while (!isComplete)
			{
				components.Dump(width, height);

				rounds++;
				isComplete = true;
				for (var y = 0; y < height; y++)
				{
					for (var x = 0; x < width; x++)
					{
						var d = y * width + x;
						var cn = ColorComponent(width, height, source, swt, components, x, y, channels);
						if (cn != components[d])
						{
							components[d] = cn;
							isComplete = false;
						}
					}
				}
			}

			return rounds;
		}

		public static void PrepareComponents(int[] swt, int[] components)
		{
			var n = components.Length;
			for (var i = 0; i < swt.Length; i++)
			{
				var stroke = swt[i];
				components[i] = stroke < int.MaxValue ? i : n + i;
			}
		}

		public static void IndexComponents(int[] components)
		{
			var n = components.Length;
			for (var i = 0; i < n; i++)
			{
				components[i] = i;
			}
		}

		public static int ColorComponentsFixedPoint(
			int width,
			int height,
			byte[] source,
			int[] swt,
			int[] components,
			int channels = 1)
		{
			Algorithms.PrepareComponents(swt, components);

			var n = components.Length;
			var rounds = 0;
			var isColored = false;
			while (!isColored)
			{
				//components.Dump(width, height);

				rounds++;
				isColored = true;
				for (var y = 0; y < height; y++)
				{
					for (var x = 0; x < width; x++)
					{
						var d = y * width + x;
						var c = components[d];
						var cn = ColorComponent(width, height, source, swt, components, x, y, channels);
						if (cn  < c)
						{
							for (var i = 0; i < 4; i++)
							{
								cn = components[Mod1(cn, n)];
							}

							components[d] = cn;
							isColored = false;
						}
					}
				}
			}

			return rounds;
		}

		public static int Mod1(int i, int mod)
		{
			if (i >= mod)
				i -= mod;
			return i;
		}

		public static int ColorComponentsFixedPointBackPropagation(
			int width,
			int height,
			byte[] source,
			int[] swt,
			int[] components,
			int channels = 1)
		{
			Algorithms.PrepareComponents(swt, components);

			var n = components.Length;
			var rounds = 0;
			var isColored = false;
			while (!isColored)
			{
				//components.Dump(width, height);

				rounds++;
				isColored = true;
				for (var y = 0; y < height; y++)
				{
					for (var x = 0; x < width; x++)
					{
						var d = y * width + x;
						var c0  = components[d];
						var cn = ColorComponent(width, height, source, swt, components, x, y, channels);
						if (cn < c0)
						{
							for (var i = 0; i < 4; i++)
							{
								var cq = components[Mod1(cn, n)];
								cn = cq;
							}

							AtomicMin(ref components[Mod1(c0, n)], cn);
							AtomicMin(ref components[d], cn);

							components[d] = cn;
							isColored = false;
						}
					}
				}
			}

			return rounds;
		}

		public static int ColorComponent(
			int width, 
			int height,
			byte[] source,
			int[] swt,
			int[] components,
			int x0, 
			int y0,
			int channels = 1)
		{
			var d = y0 * width + x0;
			var c = int.MaxValue;

			var c0 = components[d];
			var s0 = swt[d];

			Span<byte> src = stackalloc byte[channels];
			for (var channel = 0; channel < channels; channel++)
			{
				var ds = y0 * width * channels + x0 * channels + channel;
				src[channel] = source[ds];
			}

			for (var y= -1; y <= 1; y++)
			{
				if (y + y0 < 0 || y + y0 >= height) 
					continue;

				for (var x = -1; x <= 1; x++)
				{
					if (x + x0 < 0 || x + x0 >= width)
						continue;
					if (x == 0 && y == 0)
						continue;

					var dn = (y + y0) * width + x + x0;

					var cn = components[dn];
					var sn = swt[dn];
					
					if (s0 != int.MaxValue && sn != int.MaxValue)
					{
						var smin = Math.Min(s0, sn);
						var smax = Math.Max(s0, sn);
						if (smax > smin * 3)
							continue;
					}
					else
					{
						continue;
						//if (s0 == int.MaxValue || sn == int.MaxValue)
						//	continue;

						if (s0 == int.MaxValue && sn == int.MaxValue)
							continue;
						
						var sameColor = true;
						for (var channel = 0; channel < channels; channel++)
						{
							var ds = (y + y0) * width * channels + (x + x0) * channels + channel;
							var dst = source[ds];
							var diff = Math.Abs(src[channel] - dst);
							if (diff > 50)
							{
								sameColor = false;
								break;
							}
						}

						if (!sameColor)
							continue;
					}

					if (cn < c)
						c = cn;
				}
			}

			return Math.Min(c, c0);
		}

		public const int ComponentColorOffset = 0;
		public const int ComponentCountOffset = 1;
		public const int ComponentCountSwtOffset = 2;
		public const int ComponentSumSwtOffset = 3;
		public const int ComponentMinXOffset = 4;
		public const int ComponentMaxXOffset = 5;
		public const int ComponentMinYOffset = 6;
		public const int ComponentMaxYOffset = 7;
		public const int ComponentChannel1Offset = 8;
		public const int ComponentChannel2Offset = 9;
		public const int ComponentChannel3Offset = 10;
		public const int ComponentChannel4Offset = 11;
		public const int ComponentItemsOffset = 12;

		public static int ComponentAnalysis(
			int width, 
			int height,
			int[] swt, 
			int[] components, 
			int[] regionIndex, 
			int[] regions, 
			int componentLimit, 
			int componentSizeLimit)
		{
			Array.Fill(regionIndex, -1);
			for (var c = 0; c < componentLimit; c++)
			{
				regions[c * (ComponentItemsOffset + componentSizeLimit) + ComponentCountOffset] = -1;
				regions[c * (ComponentItemsOffset + componentSizeLimit) + ComponentCountSwtOffset] = -1;
				regions[c * (ComponentItemsOffset + componentSizeLimit) + ComponentSumSwtOffset] = 0;
				regions[c * (ComponentItemsOffset + componentSizeLimit) + ComponentMinXOffset] = int.MaxValue;
				regions[c * (ComponentItemsOffset + componentSizeLimit) + ComponentMaxXOffset] = int.MinValue;
				regions[c * (ComponentItemsOffset + componentSizeLimit) + ComponentMinYOffset] = int.MaxValue;
				regions[c * (ComponentItemsOffset + componentSizeLimit) + ComponentMaxYOffset] = int.MinValue;
			}

			var n = components.Length;
			var count = -1;
			var i = 0;
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++, i++)
				{
					var color = components[i];
					if (color >= n)
						continue;

					if (color == i)
						continue;

					var index = regionIndex[color];
					if (index == -1)
					{
						index = Interlocked.Increment(ref count);
						if (index >= componentLimit)
							return count;

						regionIndex[color] = index;
						regions[index * (ComponentItemsOffset + componentSizeLimit) + ComponentColorOffset] = color;
					}

					// Every region has count and a list of indexes
					var cmpIndex = index * (ComponentItemsOffset + componentSizeLimit);
					var cmpCount = Interlocked.Increment(ref regions[cmpIndex + ComponentCountOffset]);
					if (cmpCount >= componentSizeLimit)
						continue;

					var s = swt[i];
					if (s < int.MaxValue)
					{
						var swtCount = Interlocked.Increment(ref regions[cmpIndex + ComponentCountSwtOffset]);
						regions[cmpIndex + swtCount + ComponentItemsOffset] = s;

						Interlocked.Add(ref regions[cmpIndex + ComponentSumSwtOffset], s);
					}

					AtomicMin(ref regions[cmpIndex + ComponentMinXOffset], x);
					AtomicMax(ref regions[cmpIndex + ComponentMaxXOffset], x);
					AtomicMin(ref regions[cmpIndex + ComponentMinYOffset], y);
					AtomicMax(ref regions[cmpIndex + ComponentMaxYOffset], y);
				}
			}

			return count;
		}

		public static (int valid, int invalid) TextDetection(int width, int height, int count, int[] regionIndex, int[] regions, int componentSizeLimit)
		{
			var valid = 0;
			var invalid = 0;

			var n = height * width;
			for (var c = 0; c < count; c++)
			{
				var offset = c * (ComponentItemsOffset + componentSizeLimit);
				var cnt = regions[offset + ComponentCountOffset] + 1;
				Array.Sort(regions, offset + ComponentItemsOffset, cnt);

				var median = regions[offset + ComponentItemsOffset + (cnt >> 1)];
				var avg = regions[offset + ComponentSumSwtOffset] / (float)cnt;

				var items = regions.AsSpan(offset + ComponentItemsOffset, cnt);
				var variance = 0.0f;
				for (var i = 0; i < cnt; i++)
				{
					var ei = (items[i] - avg);
					variance += ei * ei;
				}
				variance /= cnt;

				var color = regions[offset + ComponentColorOffset];
				var x0 = regions[offset + ComponentMinXOffset];
				var x1 = regions[offset + ComponentMaxXOffset];
				var y0 = regions[offset + ComponentMinYOffset];
				var y1 = regions[offset + ComponentMaxYOffset];
				var w = x1 - x0;
				var h = y1 - y0;
				var sizeRatio = w / (float)h;

				var diameter = Math.Sqrt(w * w + h * h);
				var diameterRatio = diameter / median;

				if (variance < 0.5 * avg &&
				    sizeRatio > 0.1 && sizeRatio < 10 &&
				    diameterRatio < 10)
				{
					valid++;

					//for (var x = x0; x < x1; x++)
					//{
					//	text[y0 * width + x] = 0;
					//	text[y1 * width + x] = 0;
					//}

					//for (var y = y0 + 1; y < y1 - 1; y++)
					//{
					//	text[y * width + x0] = 0;
					//	text[y * width + x1] = 0;
					//}
				}
				else
				{
					invalid++;

					regionIndex[color] = -1;
				}
			}

			return (valid, invalid);
		}

		public static void AtomicMin(ref int location, int next)
		{
			while (true)
			{
				var current = location;
				next = Math.Min(current, next);
				if (current == next)
					return;

				var result = Interlocked.CompareExchange(ref location, next, current);
				if (result == current)
					return;
			}
		}

		public static void AtomicMax(ref int location, int next)
		{
			while (true)
			{
				var current = location;
				next = Math.Max(current, next);
				if (current == next)
					return;

				var result = Interlocked.CompareExchange(ref location, next, current);
				if (result == current)
					return;
			}
		}

		public static int Median(List<int> values, bool sort = true)
		{
			if (sort)
				values.Sort();

			var n = values.Count;
			var mid = n % 2 == 1 ? n / 2 : n / 2 - 1;
			if (mid < 0 ||mid >= values.Count)
				throw new ArgumentOutOfRangeException();
			return values[mid];
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

		public static void Normalize(this int[] source, byte[] destination, int channels)
		{
			var min = new int[channels];
			var max = new int[channels];

			for (var c = 0; c < channels; c++)
			{
				min[c] = int.MaxValue;
				max[c] = int.MinValue;
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

			var len = new int[channels];
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
					var r = l == 0 ? min[c] : Math.Round((v - a) / (double)l) * 255;
					destination[i + c] = Convert.ToByte(r);
				}
			}
		}

		private static int ClampToEdge(int i, int length)
		{
			if (i < 0)
				return 0;
			if (i >= length)
				return length - 1;
			return i;
		}

		private static int Clamp(int value, int max)
		{
			if (value == 0)
				return 1;
			if (value == max - 1)
				return max - 2;
			return value;
		}

		public static void Dump(this int[] data, int width, int height)
		{
			Console.WriteLine("{");
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					var v = data[d];
					Console.Write($"{(v == int.MaxValue ? 255 : v), 4}, ");
				}
				Console.WriteLine();
			}
			Console.WriteLine("}");
		}

		[Pure]
		public static byte[] ReplaceEquals(this byte[] data, byte value, byte result)
		{
			var copy = new byte[data.Length];
			Array.Copy(data, copy, data.Length);
			for (var i = 0; i < data.Length; i++)
			{
				if (copy[i] == value)
					copy[i] = result;
			}

			return copy;
		}

		[Pure]
		public static int[] ReplaceEquals(this int[] data, int value, int result)
		{
			var copy = new int[data.Length];
			Array.Copy(data, copy, data.Length);
			for (var i = 0; i < copy.Length; i++)
			{
				if (copy[i] == value)
					copy[i] = result;
			}

			return copy;
		}

		[Pure]
		public static float[] ReplaceEquals(this float[] data, float value, float result, float eps = 10e-6f)
		{
			var copy = new float[data.Length];
			Array.Copy(data, copy, data.Length);
			for (var i = 0; i < copy.Length; i++)
			{
				if (MathF.Abs(copy[i] - value) < eps)
					copy[i] = result;
			}

			return copy;
		}

		[Pure]
		public static byte[] ReplaceGreaterOrEquals(this byte[] data, byte value, byte result)
		{
			var copy = new byte[data.Length];
			Array.Copy(data, copy, data.Length);
			for (var i = 0; i < copy.Length; i++)
			{
				if (copy[i] >= value)
					copy[i] = result;
			}

			return copy;
		}

		[Pure]
		public static int[] ReplaceGreaterOrEquals(this int[] data, int value, int result)
		{
			var copy = new int[data.Length];
			Array.Copy(data, copy, data.Length);
			for (var i = 0; i < copy.Length; i++)
			{
				if (copy[i] >= value)
					copy[i] = result;
			}

			return copy;
		}

		[Pure]
		public static float[] ReplaceGreaterOrEquals(this float[] data, float value, float result)
		{
			var copy = new float[data.Length];
			Array.Copy(data, copy, data.Length);
			for (var i = 0; i < copy.Length; i++)
			{
				if (copy[i] >= value)
					copy[i] = result;
			}

			return copy;
		}
	}
}
