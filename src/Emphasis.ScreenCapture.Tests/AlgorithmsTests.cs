using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emphasis.ComputerVision;
using Emphasis.OpenCL.Extensions;
using Emphasis.ScreenCapture.Helpers;
using FluentAssertions;
using NUnit.Framework;
using RBush;
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

			Algorithms.Sobel3(width, height, source, dx, dy, gradient, angle, neighbors);

			Run("sample00.png");
			gradient.RunAs(width, height, 1, "sobel_gradient.png");

			//source.RunAsText(width, height, 4, "sample00.txt");
			//gradient.RunAsText(width, height, 1, "sobel_gradient.txt");
		}

		[Test]
		public void Enlarge_Test()
		{
			var sourceBitmap = Samples.sample03;

			Run("sample03.png");

			var source = sourceBitmap.ToBytes();
			var channels = 4;

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var large = new byte[height * width * 4 * channels];
			Algorithms.Enlarge2(width, height, source, large, channels);

			large.RunAs(width * 2, height * 2, channels, "large.png");
		}

		[Test]
		public void NonMaximumSuppression_Test()
		{
			var sourceBitmap = Samples.sample13;

			//Run("sample03.png");

			var source = sourceBitmap.ToBytes();
			var channels = 4;

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var useLarge = true;
			var swtEdgeOnColorChange = true;
			var swtEdgeColorTolerance = 50;
			var swtConnectByColor = false;
			var varianceTolerance = 2.0f;
			var sizeRatioTolerance = 10;

			var large = new byte[height * width * 4 * channels];
			Algorithms.Enlarge2(width, height, source, large, channels);

			if (useLarge)
			{
				width *= 2;
				height *= 2;
			}
			var src = useLarge ? large : source;


			var n = height * width;

			var linePrefix = new int[n * channels];
			Span<int> prefix = stackalloc int[channels];
			for (var y = 0; y < height; y++)
			{

				for (var x = 0; x < width; x++)
				{
					for (var c = 0; c < channels; c++)
					{
						var d = y * width * channels + x * channels + c;
						var v = src[d];
						var r = prefix[c] += v;
						linePrefix[d] = r;
					}
				}
			}

			var bw = 15;
			var bw2 = bw * 2 + 1;
			var box = new byte[n * channels];
			Span<int> di = stackalloc int[bw2];
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var x1 = Math.Max(0, x - bw);
					var x2 = Math.Min(width - 1, x + bw);
					for (var c = 0; c < channels; c++)
					{
						var d = y * width * channels + x * channels + c;
						var y1 = Math.Max(0, y - bw);
						var y2 = Math.Min(height - 1, y + bw);
						var sum = 0;
						for (var yi = y1; yi <= y2; yi++)
						{
							var d1 = yi * width * channels + x1 * channels + c;
							var d2 = yi * width * channels + x2 * channels + c;
							var diff = linePrefix[d2] - linePrefix[d1];
							sum += diff;
						}

						var avg = sum / ((y2 - y1) * (x2 - x1));
						box[d] = (byte) Math.Min(255, avg);
					}
				}
			}

			box.RunAs(width, height, channels, $"box{bw}.png");

			var gauss = new byte[n * channels];
			var grayscale = new byte[n];

			var gradient = new float[n];
			var dx = new float[n];
			var dy = new float[n];
			var angle = new float[n];
			var neighbors = new byte[n * 5];
			var nms = new float[n];
			var cmp0 = new float[n];
			var cmp1 = new float[n];
			var swt0 = new int[n];
			var swt1 = new int[n];
			
			Array.Fill(swt0, int.MaxValue);
			Array.Fill(swt1, int.MaxValue);

			Algorithms.Grayscale(width, height, src, grayscale);
			Algorithms.Gauss(width, height, src, gauss);
			Algorithms.Sobel3(width, height, gauss,  dx, dy, gradient, angle, neighbors);
			Algorithms.NonMaximumSuppression(width, height, gradient, angle, neighbors, nms, cmp0, cmp1);
			Algorithms.StrokeWidthTransform(width, height, src, gradient, nms, angle, dx, dy, swt0, swt1,
				sourceChannels: 4,
				rayLength: 30,
				colorDifference: swtEdgeColorTolerance,
				useStrokeColor: swtEdgeOnColorChange);

			var components0 = new int[height * width];
			var components1 = new int[height * width];

			var colorRounds0 = Algorithms.ColorComponentsFixedPointBackPropagation(width, height, src, swt0, components0, null,
				sourceChannels: 4, connectByColor: false);
			var colorRounds1 = Algorithms.ColorComponentsFixedPointBackPropagation(width, height, src, swt1, components1, null,
				sourceChannels: 4, connectByColor: false);

			var regionIndex0 = new int[n];
			var regionIndex1 = new int[n];

			var componentLimit = 100000;
			var componentSizeLimit = 1024;
			var cn = componentLimit * componentSizeLimit;

			var regions0 = new int[cn];
			var regions1 = new int[cn];

			var regionSwt0 = new int[cn];
			var regionSwt1 = new int[cn];

			var componentList0 = new Component[componentLimit];
			var componentList1 = new Component[componentLimit];

			var regionCount0 = Algorithms.ComponentAnalysis(
				width, height, src, swt0, components0, regionIndex0, regions0, regionSwt0, componentList0, componentLimit, componentSizeLimit, sourceChannels: channels);
			var regionCount1 = Algorithms.ComponentAnalysis(
				width, height, src, swt1, components1, regionIndex1, regions1, regionSwt0, componentList1, componentLimit, componentSizeLimit, sourceChannels: channels);

			// Filter components 1st pass
			var valid0 = Algorithms.PassiveFilter(regionCount0, componentList0);
			var valid1 = Algorithms.PassiveFilter(regionCount1, componentList1);

			var invalid0 = regionCount0 - valid0;
			var invalid1 = regionCount1 - valid1;

			//var rtree0 = Algorithms.ComponentRBush(regionCount0, componentList0);
			//var rtree1 = Algorithms.ComponentRBush(regionCount1, componentList1);

			//Algorithms.MergeComponents(width, height, regionCount0, componentList0, rtree0);
			//Algorithms.MergeComponents(width, height, regionCount1, componentList1, rtree1);

			//Algorithms.RemoveBoxes(width, height, regionCount0, componentList0, rtree0);
			//Algorithms.RemoveBoxes(width, height, regionCount1, componentList1, rtree1);

			large.RunAs(width, height, channels, "large.png");
			//large.RunAsText(width, height, channels, "large.txt");

			//gauss.RunAs(width, height, channels, "gauss.png");

			//grayscale.RunAs(width, height, 1, "gray.png");
			//grayscale.ReplaceEquals(255, 0).RunAsText(width, height, 1, "gray.txt");

			//gradient.RunAs(width, height, 1, "gradient.png");
			gradient.RunAsText(width, height, 1, "gradient.txt");

			//angle.RunAsText(width, height, 1, "angle.txt");

			//nms.RunAs(width, height, 1, "nms.png");
			nms.RunAsText(width, height, 1, "nms.txt");

			swt0.RunAs(width, height, 1, "swt0.png");
			swt0.ReplaceEquals(int.MaxValue, 0).MultiplyBy(10).RunAsText(width, height, 1, "swt0.txt");

			swt1.RunAs(width, height, 1, "swt1.png");
			swt1.ReplaceEquals(int.MaxValue, 0).MultiplyBy(10).RunAsText(width, height, 1, "swt1.txt");

			components0.RunAs(width, height, 1, "cc0.png");
			components0.ReplaceGreaterOrEquals(n, 0).RunAsText(width, height, 1, "cc0.txt");

			components1.RunAs(width, height, 1, "cc1.png");
			components1.ReplaceGreaterOrEquals(n, 0).RunAsText(width, height, 1, "cc1.txt");

			Console.WriteLine($"Components BoW: {valid0}/{invalid0}");
			Console.WriteLine($"Components WoB: {valid1}/{invalid1}");

			var text0 = new int[n];
			var text1 = new int[n];
			for (var i = 0; i < n; i++)
			{
				var color = components0[i];
				if (color >= n)
				{
					text0[i] = 255;
					continue;
				}

				var ci = regionIndex0[color];
				if (color >= n || ci == -1)
				{
					text0[i] = 255;
					continue;
				}
				ref var c = ref componentList0[ci];
				if (!c.IsValid())
					text0[i] = c.Validity;
			}

			for (var i = 0; i < n; i++)
			{
				var color = components1[i];
				if (color >= n)
				{
					text1[i] = 255;
					continue;
				}

				var ci = regionIndex1[color];
				if (color >= n || ci == -1)
				{
					text1[i] = 255;
					continue;
				}
				ref var c = ref componentList1[ci];
				if (!c.IsValid())
					text1[i] = c.Validity;
			}

			text0.ReplaceGreaterOrEquals(1,255).RunAs(width, height, 1, "text0.png");
			text0.RunAsText(width, height, 1, "text0.txt");
			text1.ReplaceGreaterOrEquals(1, 255).RunAs(width, height, 1, "text1.png");
			text1.RunAsText(width, height, 1, "text1.txt");
		}

		[Test]
		public void RBush_Test()
		{
			var b0 = new Box2D(0, 10, 0, 10);
			var b1 = new Box2D(0, 0, 0, 0);
			var b2 = new Box2D(0, 1, 0, 1);
			var b3 = new Box2D(10, 10, 10, 10);
			var b4 = new Box2D(10, 11, 10, 11);
			var b5 = new Box2D(5, 15, 5, 15);
			var b6 = new Box2D(11, 11, 11, 11);

			var rtree = new RBush<Box2D>();
			rtree.BulkLoad(new[] {b0, b1, b2, b3, b4, b5, b6});

			var content = rtree.Search(b0.Envelope).ToArray();

			// Contains even partial matches
			content.Should().Contain(new[] {b0, b1, b2, b3, b4, b5});
			content.Should().NotContain(new[] {b6});
		}

		[Test]
		public void ConnectedComponentsAnalysis_multiple_components_Test()
		{
			var max = int.MaxValue;

			var width = 14;
			var height = 9;
			var n = height * width;

			var swt = new int[]
			{
				max, max, max,   3, max, max,   1, max, max, max, max,   1,   1,   1,
				max, max,   2,   2,   1, max,   1, max, max, max, max,   1,   1,   1,
				max, max, max, max,   4, max, max,   1, max, max, max,   1,   1,   1, 
				max, max, max, max, max,   2, max, max,   1, max, max, max,   1, max,
				  1, max, max, max, max, max, max,   1, max,   1, max,   1, max, max,
				 10,   3, max, max, max, max,   1, max, max, max,   1, max, max, max,
				max,   2,   5, max, max, max,   1, max, max, max, max, max, max, max,
				  4, max,   7,   3, max, max, max,   1, max, max,   1,   1,   1,   2,
				max, max, max,   1,   1,   1, max, max,   1,   1, max, max, max, max,
			};

			var source = new byte[]
			{
				255, 255, 255,   3, 255, 255,   1, 255, 255, 255, 255,   1,   1,   1,
				255, 255,   2,   2,   1, 255,   1, 255, 255, 255, 255,   1,   1,   1,
				255, 255,   1, 255,   4, 255, 255,   1, 255, 255, 255,   1,   1,   1,
				255, 255, 255, 255, 255,   2, 255, 255,   1, 255, 255, 255,   1, 255,
				  1, 255, 255, 255, 255, 255, 255,   1, 255,   1, 255,   1, 255, 255,
				 50,   3, 255, 255, 255, 255,   1, 255, 255, 255,   1, 255, 255, 255,
				255,   2,   5, 255, 255, 255,   1, 255, 255, 255, 255, 255, 255, 255,
				  4, 255,   7,   3, 255, 255, 255,   1, 255, 255,   1,   1,   1,   2,
				255, 255, 255,   1,   1,   1, 255, 255,   1,   1, 255, 255, 255, 255,
			};

			swt.Length.Should().Be(width * height);

			var components = new int [height * width];
			
			var rounds = Algorithms.ColorComponentsFixedPoint(width, height, source, swt, components);

			var r0 = height * width;
			var result = new int[]
			{
				 r0,  r0,  r0,   3,  r0,  r0,   6,  r0,  r0,  r0,  r0,   6,   6,   6,
				 r0,  r0,   3,   3,   3,  r0,   6,  r0,  r0,  r0,  r0,   6,   6,   6,
				 r0,  r0,  r0,  r0,   3,  r0,  r0,   6,  r0,  r0,  r0,   6,   6,   6,
				 r0,  r0,  r0,  r0,  r0,   3,  r0,  r0,   6,  r0,  r0,  r0,   6,  r0,
				 56,  r0,  r0,  r0,  r0,  r0,  r0,   6,  r0,   6,  r0,   6,  r0,  r0,
				 70,  56,  r0,  r0,  r0,  r0,   6,  r0,  r0,  r0,   6,  r0,  r0,  r0,
				 r0,  56,  56,  r0,  r0,  r0,   6,  r0,  r0,  r0,  r0,  r0,  r0,  r0,
				 56,  r0,  56,  56,  r0,  r0,  r0,   6,  r0,  r0,   6,   6,   6,   6,
				 r0,  r0,  r0,  56,  56,  56,  r0,  r0,   6,   6,  r0,  r0,  r0,  r0,
			};

			for (var i = 0; i < n; i++)
			{
				if (result[i] >= n)
					result[i] = n + i;
			}

			Algorithms.Dump(result, width, height);
			Algorithms.Dump(components, width, height);

			components.Should().Equal(result);
			Console.WriteLine($"Rounds: {rounds}");

			var componentLimit = 1024;
			var componentSizeLimit = 6;
			var cn = componentLimit * componentSizeLimit;
			var regionIndex = new int[height * width];
			var regions = new int[cn];
			var regionSwt = new int[cn];
			var componentList = new Component[componentLimit];

			var regionCount = Algorithms.ComponentAnalysis(
				width, height, source, swt, components, regionIndex, regions, regionSwt, componentList, componentLimit, componentSizeLimit, sourceChannels: 1);

			regionCount.Should().Be(3);
			regionIndex[6].Should().Be(0);
			regionIndex[3].Should().Be(1);
			regionIndex[56].Should().Be(2);

			regions[Algorithms.ComponentCountOffset].Should().Be(25);
			regions[Algorithms.ComponentCountSwtOffset].Should().Be(5);
			regions[Algorithms.ComponentSumSwtOffset].Should().Be(6);
			regions[Algorithms.ComponentMinXOffset].Should().Be(6);
			regions[Algorithms.ComponentMaxXOffset].Should().Be(13);
			regions[Algorithms.ComponentMinYOffset].Should().Be(0);
			regions[Algorithms.ComponentMaxYOffset].Should().Be(1);

			//regions.AsSpan(Algorithms.ComponentItemsOffset, componentSizeLimit).ToArray().Should().Equal(6, 11, 12, 13, 20, 25);
			regions.AsSpan(Algorithms.ComponentItemsOffset, componentSizeLimit).ToArray().Should().Equal(1, 1, 1, 1, 1, 1);

			var ci = componentSizeLimit + Algorithms.ComponentItemsOffset;
			regions[ci + Algorithms.ComponentCountOffset].Should().Be(4); // count -2
			regions[ci + Algorithms.ComponentCountSwtOffset].Should().Be(4); // swt count -2
			regions[ci + Algorithms.ComponentSumSwtOffset].Should().Be(11); // sum
			regions[ci + Algorithms.ComponentMinXOffset].Should().Be(2); // min x
			regions[ci + Algorithms.ComponentMaxXOffset].Should().Be(5); // max x
			regions[ci + Algorithms.ComponentMinYOffset].Should().Be(1); // min y
			regions[ci + Algorithms.ComponentMaxYOffset].Should().Be(3); // max y

			//regions.AsSpan(ci + Algorithms.ComponentItemsOffset, componentSizeLimit).ToArray().Should().Equal(3, 16, 17, 18, 32, 47);
			var reg1 = regions.AsSpan(ci + Algorithms.ComponentItemsOffset, componentSizeLimit).ToArray();
			reg1.Should().Equal(2, 2, 1, 4, 2, 0);
		}

		[Test]
		public void ConnectedComponentsAnalysis()
		{
			var max = int.MaxValue;
			var width = 10;
			var height = 10;
			var n = height * width;

			var values = new int[]
			{
				max, max, max, max, max, max, max, max, max, max,
				max,   1, max, max, max,   1, max, max,   1, max,
				max,   1, max, max, max,   1, max, max,   1, max,
				max,   1, max, max,   1,   1, max, max,   1, max,
				max,   1, max,   1,   1,   1, max, max,   1, max,
				max,   1, max,   1, max,   1, max,   1,   1, max,
				max,   1,   1,   1, max,   1,   1,   1, max, max,
				max,   1,   1, max, max,   1,   1, max, max, max,
				max,   1, max, max, max,   1, max, max, max, max,
				max, max, max, max, max, max, max, max, max, max,
			};

			var source = new byte[]
			{
				255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
				255,   1,   1, 255, 255,   1, 255, 255,   1, 255,
				255,   1, 255, 255, 255,   1, 255, 255,   1, 255,
				255,   1, 255, 255,   1,   1, 255, 255,   1, 255,
				255,   1, 255,   1,   1,   1, 255, 255,   1, 255,
				255,   1, 255,   1, 255,   1, 255,   1,   1, 255,
				255,   1,   1,   1, 255,   1,   1,   1, 255, 255,
				255,   1,   1, 255, 255,   1,   1, 255, 255, 255,
				255,   1, 255, 255, 255,   1, 255, 255, 255, 255,
				255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
			};

			var components0 = new int[height * width];
			var components1 = new int[height * width];
			var components2 = new int[height * width];

			var roundsWatershed = Algorithms.ColorComponentsWatershed(width, height, source, values, components0);
			var roundsFixedPoint = Algorithms.ColorComponentsFixedPoint(width, height, source, values, components1);
			var roundsBackPropagation = Algorithms.ColorComponentsFixedPointBackPropagation(width, height, source, values, components2);

			var r0 = height * width;
			var result = new int[]
			{
				 r0,  r0,  r0,  r0,  r0,  r0,  r0,  r0,  r0,  r0,
				 r0,  11,  11,  r0,  r0,  11,  r0,  r0,  11,  r0,
				 r0,  11,  r0,  r0,  r0,  11,  r0,  r0,  11,  r0,
				 r0,  11,  r0,  r0,  11,  11,  r0,  r0,  11,  r0,
				 r0,  11,  r0,  11,  11,  11,  r0,  r0,  11,  r0,
				 r0,  11,  r0,  11,  r0,  11,  r0,  11,  11,  r0,
				 r0,  11,  11,  11,  r0,  11,  11,  11,  r0,  r0,
				 r0,  11,  11,  r0,  r0,  11,  11,  r0,  r0,  r0,
				 r0,  11,  r0,  r0,  r0,  11,  r0,  r0,  r0,  r0,
				 r0,  r0,  r0,  r0,  r0,  r0,  r0,  r0,  r0,  r0,
			};

			for (var i = 0; i < n; i++)
			{
				if (components0[i] >= n)
					components0[i] = r0;
				if (components1[i] >= n)
					components1[i] = r0;
				if (components2[i] >= n)
					components2[i] = r0;
			}

			components0.Should().Equal(result);
			components1.Should().Equal(result);
			components2.Should().Equal(result);

			Console.WriteLine($"Rounds Watershed:       {roundsWatershed}");
			Console.WriteLine($"Rounds FixedPoint:      {roundsFixedPoint}");
			Console.WriteLine($"Rounds BackPropagation: {roundsBackPropagation}");

			roundsFixedPoint.Should().BeLessThan(roundsWatershed);
			roundsBackPropagation.Should().BeLessThan(roundsFixedPoint);
		}

		[Test]
		public void Canny()
		{
			var sourceBitmap = Samples.sample03;

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

			Run("sample03.png");
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
