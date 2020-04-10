using System;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emphasis.ComputerVision;
using Emphasis.ScreenCapture.Helpers;
using FluentAssertions;
using NUnit.Framework;

using static Emphasis.ScreenCapture.Helpers.DebugHelper;

namespace Emphasis.ScreenCapture.Tests
{
	public class AlgorithmsTests
	{
		[Test]
		public void Gauss_Test()
		{
			var sourceBitmap = Samples.sample02;
			var source = sourceBitmap.ToBytes();

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var result = new byte[source.Length];
			Algorithms.Gauss(width, height, source, result);

			Run("sample02.png");

			result.RunAs(width, height, 4, "gauss.png");
		}

		[Test]
		public void Sobel_Test()
		{
			var sourceBitmap = Samples.sample00;
			var source = sourceBitmap.ToBytes();

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var neighbors = new byte[height * width * 5];
			var gradient = new float[height * width];
			var angle = new float[height * width];
			var dx = new float[height * width];
			var dy = new float[height * width];

			Algorithms.Sobel(width, height, source, dx, dy, gradient, angle, neighbors);

			Run("sample00.png");
			gradient.RunAs(width, height, 1, "sobel_gradient.png");

			//source.RunAsText(width, height, 4, "sample00.txt");
			//gradient.RunAsText(width, height, 1, "sobel_gradient.txt");
		}

		[Test]
		public async Task NonMaximumSuppression_Test()
		{
			var sourceBitmap = Samples.sample03;

			var source = sourceBitmap.ToBytes();
			var gauss = new byte[source.Length];

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var grayscale = new byte[height * width];

			var gradient = new float[height * width];
			var dx = new float[height * width];
			var dy = new float[height * width];
			var angle = new float[height * width];
			var neighbors = new byte[height * width * 5];
			var nms = new float[height * width];
			var cmp1 = new float[height * width];
			var cmp2 = new float[height * width];
			var swt0 = new float[height * width];
			var swt1 = new float[height * width];
			
			Array.Fill(swt0, float.MaxValue);
			Array.Fill(swt1, float.MaxValue);

			Algorithms.Grayscale(width,height, source, grayscale);

			Algorithms.Gauss(width, height, source, gauss);

			Algorithms.Sobel(width, height, source,  dx, dy, gradient, angle, neighbors);

			Algorithms.NonMaximumSuppression(width, height, gradient, angle, neighbors, nms, cmp1, cmp2);

			Algorithms.StrokeWidthTransform(width, height, nms, angle, dx, dy, swt0, swt1);

			var cc0 = new int[height * width];
			var cc1 = new int[height * width];
			Algorithms.ColorComponentsFixedPoint(width, height, swt0, cc0);
			Algorithms.ColorComponentsFixedPoint(width, height, swt1, cc1);

			Run("sample03.png");

			grayscale.RunAs(width, height, 1, "grayscale.png");
			gradient.RunAs(width, height, 1, "sobel_gradient.png");

			var dxa = new float[height * width];
			var dya = new float[height * width];
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < height; x++)
				{
					var d = y * width + x;
					dxa[d] = MathF.Abs(dx[d]);
					dya[d] = MathF.Abs(dy[d]);
				}
			}

			dxa.RunAs(width, height, 1, "dxa.png");
			dya.RunAs(width, height, 1, "dya.png");

			nms.RunAs(width, height, 1, "sobel_gradient_nms.png");
			swt0.RunAs(width, height, 1, "swt0.png");
			swt1.RunAs(width, height, 1, "swt1.png");

			cc0.RunAs(width, height, 1, "cc0.png");
			cc1.RunAs(width, height, 1, "cc1.png");

			//source.RunAsText(width, height, 4, "sample00.txt");
			//await Task.Delay(100);

			//grayscale.RunAsText(width, height, 1, "grayscale.txt");
			//await Task.Delay(100);

			//angle.RunAsText(width, height, 1, "sobel_angle.txt");
			//await Task.Delay(100);

			//direction.RunAsText(width, height, 1, "sobel_direction.txt");
			//await Task.Delay(100);

			//gradient.RunAsText(width, height, 1, "sobel_gradient.txt");
			//await Task.Delay(100);

			//gradientNms.RunAsText(width, height, 1, "sobel_gradient_nms.txt");
			//await Task.Delay(100);

			//cmp1.RunAsText(width, height, 1, "cmp1.txt");
			//await Task.Delay(100);

			//cmp2.RunAsText(width, height, 1, "cmp2.txt");
			//await Task.Delay(100);
		}

		[Test]
		public void ConnectedComponentsAnalysis_multiple_components_Test()
		{
			var width = 14;
			var height = 9;
			var values = new float[]
			{
				255, 255, 255,   3, 255, 255,   1, 255, 255, 255, 255,   1,   1,   1,
				255, 255,   2,   2,   1, 255,   1, 255, 255, 255, 255,   1,   1,   1,
				255, 255, 255, 255,   4, 255, 255,   1, 255, 255, 255,   1,   1,   1, 
				255, 255, 255, 255, 255,   2, 255, 255,   1, 255, 255, 255,   1, 255,
				  1, 255, 255, 255, 255, 255, 255,   1, 255,   1, 255,   1, 255, 255,
				 10,   3, 255, 255, 255, 255,   1, 255, 255, 255,   1, 255, 255, 255,
				255,   2,   5, 255, 255, 255,   1, 255, 255, 255, 255, 255, 255, 255,
				  4, 255,   7,   3, 255, 255, 255,   1, 255, 255,   1,   1,   1,   1,
				255, 255, 255,   1,   1,   1, 255, 255,   1,   1, 255, 255, 255, 255,
			};
			values.Length.Should().Be(width * height);

			var components = new int [height * width];
			Algorithms.IndexComponents(components);
			var rounds = Algorithms.ColorComponentsFixedPoint(width, height, values, components, limit: 128);

			var max = int.MaxValue;
			var result = new int[]
			{
				max, max, max,   3, max, max,   6, max, max, max, max,   6,   6,   6,
				max, max,   3,   3,   3, max,   6, max, max, max, max,   6,   6,   6,
				max, max, max, max,   3, max, max,   6, max, max, max,   6,   6,   6,
				max, max, max, max, max,   3, max, max,   6, max, max, max,   6, max,
				 56, max, max, max, max, max, max,   6, max,   6, max,   6, max, max,
				max,  56, max, max, max, max,   6, max, max, max,   6, max, max, max,
				max,  56,  56, max, max, max,   6, max, max, max, max, max, max, max,
				 56, max,  56,  56, max, max, max,   6, max, max,   6,   6,   6,   6,
				max, max, max,  56,  56,  56, max, max,   6,   6, max, max, max, max,
			};

			components.Should().Equal(result);
			Console.WriteLine($"Rounds: {rounds}");

			var componentLimit = 1024;
			var componentSizeLimit = 50;
			var regionIndex = new int[height * width];
			var regions = new int[componentLimit * (1 + componentSizeLimit)];
			Algorithms.ComponentAnalysis(components, regionIndex, regions, componentLimit, componentSizeLimit);
		}

		[Test]
		public void Canny()
		{
			var sourceBitmap = Samples.sample01;

			var source = sourceBitmap.ToBytes();

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var data = new byte[height, width, 4];
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * (width * 4) + x * 4;
					for (var c = 0; c < 4; c++)
					{
						data[y, x, c] = source[d + c];
					}
				}
			}

			var image = new Image<Bgra, byte>(data);
			var canny = image.Canny(50, 20, 5, false);
			//var laplace = image.Laplace(3);
			//var sobel = image.Sobel(1, 1, 5);

			var result = new float[height * width];
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					result[d] = canny.Data[y, x, 0];
				}
			}

			Run("sample01.png");
			result.RunAs(width, height, 1, "canny.png");
		}

		[Test]
		public void Line_Test()
		{
			var width = 100;
			var height = 100;
			var data = new byte[height * width];

			var x = 50;
			var y = 50;

			var dx = 5;
			var dy = 0;

			var errx = dx;
			var erry = dy;

			while (x >= 0 && x < width && y >= 0 && y < height)
			{
				var d = y * width + x;
				data[d] = 255;

				if (errx >= erry)
				{
					erry += dy;
					x += 1;
				}
				else
				{
					errx += dx;
					y -= 1;
				}
			}

			data.RunAs(width,height, 1, "line.png");
		}

		[Test]
		public void Atan2PiTo8way_Test()
		{
			for (var i = -180; i <= 180; i += 1)
			{
				var a = i / 180.0f;
				var b = ((((a + 2) % 2) + 0.125f) * 4 - 0.5f) % 7;
				Console.WriteLine($"{i,4} {Convert.ToByte(MathF.Round(b))} {a}");
			}
		}

		[Test]
		public void Atan2_Test()
		{
			Console.WriteLine(MathF.Atan2(-100, +50) / MathF.PI * 180);
		}

		[Test]
		public void GradientNeighbors_Test()
		{
			for (var i = 0.0f; i <= 360.0f; i++)
			{
				var n = Algorithms.GradientNeighbors(i);

				if (i < 11.25f || i > 348.75f)
				{
					n.Should().Be((1, 3, 25, 50, 25));
				}
				else if (i > 11.25f && i < 33.75f)
				{
					n.Should().Be((1, 2, 50, 50, 0));
				}
				else if (i > 33.75f && i < 56.25f)
				{
					n.Should().Be((2, 3, 25, 50, 25));
				}
				else if (i > 56.25f && i < 78.75f)
				{
					n.Should().Be((2, 2, 50, 50, 0));
				}
				else if (i > 78.75f && i < 101.25f)
				{
					n.Should().Be((3, 3, 25, 50, 25));
				}
				else if (i > 101.25f && i < 123.75f)
				{
					n.Should().Be((3, 2, 50, 50, 0));
				}
				else if (i > 123.75f && i < 146.25f)
				{
					n.Should().Be((4, 3, 25, 50, 25));
				}
				else if (i > 146.25f && i < 168.75f)
				{
					n.Should().Be((4, 2, 50, 50, 0));
				}
				else if (i > 168.75f && i < 191.25f)
				{
					n.Should().Be((5, 3, 25, 50, 25));
				}
				else if (i > 191.25f && i < 213.75f)
				{
					n.Should().Be((5, 2, 50, 50, 0));
				}
				else if (i > 213.75f && i < 236.25f)
				{
					n.Should().Be((6, 3, 25, 50, 25));
				}
				else if (i > 236.25f && i < 258.75f)
				{
					n.Should().Be((6, 2, 50, 50, 0));
				}
				else if (i > 258.75f && i < 281.25f)
				{
					n.Should().Be((7, 3, 25, 50, 25));
				}
				else if (i > 281.25f && i < 303.75f)
				{
					n.Should().Be((7, 2, 50, 50, 0));
				}
				else if (i > 303.75f && i < 326.25f)
				{
					n.Should().Be((8, 3, 25, 50, 25));
				}
				else if (i > 326.25f && i < 348.75f)
				{
					n.Should().Be((8, 2, 50, 50, 0));
				}
			}
		}
	}
}
