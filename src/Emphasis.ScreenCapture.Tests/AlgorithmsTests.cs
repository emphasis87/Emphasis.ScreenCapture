using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emphasis.ComputerVision;
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
			var sourceBitmap = Samples.sample11;

			//Run("sample03.png");

			var source = sourceBitmap.ToBytes();
			var channels = 4;

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var useLarge = true;
			var swtEdgeOnColorChange = false;
			var swtEdgeColorTolerance = 50;
			var swtConnectByColor = false;
			var varianceTolerance = 2.0f;

			var large = new byte[height * width * 4 * channels];
			Algorithms.Enlarge2(width, height, source, large, channels);

			if (useLarge)
			{
				width *= 2;
				height *= 2;
			}
			var n = height * width;

			var gauss = new byte[n * channels];
			var grayscale = new byte[n];

			var gradient = new float[n];
			var dx = new float[n];
			var dy = new float[n];
			var angle = new float[n];
			var neighbors = new byte[n * 5];
			var nms = new float[n];
			var cmp1 = new float[n];
			var cmp2 = new float[n];
			var swt0 = new int[n];
			var swt1 = new int[n];
			
			Array.Fill(swt0, int.MaxValue);
			Array.Fill(swt1, int.MaxValue);

			var src = useLarge ? large : source;

			Algorithms.Grayscale(width, height, src, grayscale);
			Algorithms.Gauss(width, height, src, gauss);
			Algorithms.Sobel3(width, height, gauss,  dx, dy, gradient, angle, neighbors);
			Algorithms.NonMaximumSuppression(width, height, gradient, angle, neighbors, nms, cmp1, cmp2);
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

			var regions0 = new int[componentLimit * (Algorithms.ComponentItemsOffset + componentSizeLimit)];
			var regions1 = new int[componentLimit * (Algorithms.ComponentItemsOffset + componentSizeLimit)];

			var regionCount0 = Algorithms.ComponentAnalysis(
				width, height, src, swt0, components0, regionIndex0, regions0, componentLimit, componentSizeLimit, sourceChannels: channels);
			var regionCount1 = Algorithms.ComponentAnalysis(
				width, height, src, swt1, components1, regionIndex1, regions1, componentLimit, componentSizeLimit, sourceChannels: channels);

			if (swtConnectByColor)
			{
				// Compute color for each component
				var avgColor0 = new int[regionCount0 * channels];
				var avgColor1 = new int[regionCount1 * channels];
				for (var r = 0; r < regionCount0; r++)
				{
					var swtCountOffset = Algorithms.GetComponentSwtCountOffset(r, componentSizeLimit);
					var channelOffset = Algorithms.GetComponentChannel0Offset(r, componentSizeLimit);
					for (var c = 0; c < channels; c++)
					{
						var color = regions0[channelOffset + c];
						var swtCount = regions0[swtCountOffset] + 1;
						avgColor0[r * channels + c] = color / swtCount;
					}
				}

				for (var r = 0; r < regionCount1; r++)
				{
					var swtCountOffset = Algorithms.GetComponentSwtCountOffset(r, componentSizeLimit);
					var channelOffset = Algorithms.GetComponentChannel0Offset(r, componentSizeLimit);
					for (var c = 0; c < channels; c++)
					{
						var color = regions1[channelOffset + c];
						var swtCount = regions1[swtCountOffset] + 1;
						avgColor1[r * channels + c] = color / swtCount;
					}
				}

				components0.RunAs(width, height, 1, "cc0-1st.png");
				components0.ReplaceGreaterOrEquals(n, 0).RunAsText(width, height, 1, "cc0-1nd.txt");

				colorRounds0 += Algorithms.ColorComponentsFixedPointBackPropagation(width, height, src, swt0,
					components0, regionIndex0, avgColor0,
					sourceChannels: 4, connectByColor: true);
				colorRounds1 += Algorithms.ColorComponentsFixedPointBackPropagation(width, height, src, swt1,
					components1, regionIndex1, avgColor1,
					sourceChannels: 4, connectByColor: true);

				regionCount0 = Algorithms.ComponentAnalysis(
					width, height, src, swt0, components0, regionIndex0, regions0, componentLimit, componentSizeLimit,
					sourceChannels: channels);
				regionCount1 = Algorithms.ComponentAnalysis(
					width, height, src, swt1, components1, regionIndex1, regions1, componentLimit, componentSizeLimit,
					sourceChannels: channels);
			}

			var result0 = new int[componentLimit];
			var result1 = new int[componentLimit];
			var (valid0, invalid0) = Algorithms.TextDetection(
				width, height, regionCount0, regionIndex0, regions0, result0, componentSizeLimit,
				varianceTolerance: varianceTolerance);
			var (valid1, invalid1) = Algorithms.TextDetection(
				width, height, regionCount1, regionIndex1, regions1, result1, componentSizeLimit,
				varianceTolerance: varianceTolerance);

			large.RunAs(width, height, channels, "large.png");
			//large.RunAsText(width, height, channels, "large.txt");

			//gauss.RunAs(width, height, channels, "gauss.png");

			//grayscale.RunAs(width, height, 1, "gray.png");
			//grayscale.ReplaceEquals(255, 0).RunAsText(width, height, 1, "gray.txt");

			//gradient.RunAs(width, height, 1, "gradient.png");
			//gradient.RunAsText(width, height, 1, "gradient.txt");

			//angle.RunAsText(width, height, 1, "angle.txt");

			//nms.RunAs(width, height, 1, "nms.png");
			//nms.RunAsText(width, height, 1, "nms.txt");

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
				if (color >= n || regionIndex0[color] == -1)
					text0[i] = 255;
			}
			for (var i = 0; i < n; i++)
			{
				var color = components1[i];
				if (color >= n || regionIndex1[color] == -1)
					text1[i] = 255;
			}

			text0.RunAs(width, height, 1, "text0.png");
			text1.RunAs(width, height, 1, "text1.png");

			var rtree0 = new RBush<Point2D>();
			var points = new List<Point2D>(valid0);
			for (var i = 0; i < valid0; i++)
			{
				var color = regions0[Algorithms.GetComponentColorOffset(i, componentSizeLimit)];
				var x0 = regions0[Algorithms.GetComponentMinXOffset(i, componentSizeLimit)];
				var x1 = regions0[Algorithms.GetComponentMaxXOffset(i, componentSizeLimit)];
				var y0 = regions0[Algorithms.GetComponentMinXOffset(i, componentSizeLimit)];
				var y1 = regions0[Algorithms.GetComponentMaxXOffset(i, componentSizeLimit)];
				var p = new Point2D(x0, x1, y0, y1, color);
				points.Add(p);
			}
			rtree0.BulkLoad(points);

			// Horizontal lines
			var hl0 = result0.Take(valid0).OrderBy(c => regions0[Algorithms.GetComponentMinXOffset(c, componentSizeLimit)]).ToArray();
			var hl1 = result1.Take(valid1).OrderBy(c => regions1[Algorithms.GetComponentMinXOffset(c, componentSizeLimit)]).ToArray();

			// Reverse indexing
			var xl0 = new int[componentLimit];
			var xl1 = new int[componentLimit];

			for (var i = 0; i < valid0; i++)
			{
				var ci = hl0[i];
				xl0[ci] = i;
			}

			for (var i = 0; i < valid1; i++)
			{
				var ci = hl1[i];
				xl1[ci] = i;
			}

			// Vertical lines
			var vl0 = result0.Take(valid0).OrderBy(c => regions0[Algorithms.GetComponentMinYOffset(c, componentSizeLimit)]).ToArray();
			var vl1 = result1.Take(valid1).OrderBy(c => regions1[Algorithms.GetComponentMinYOffset(c, componentSizeLimit)]).ToArray();

			// Reverse indexing
			var yl0 = new int[componentLimit];
			var yl1 = new int[componentLimit];

			for (var i = 0; i < valid0; i++)
			{
				var ci = vl0[i];
				yl0[ci] = i;
			}

			for (var i = 0; i < valid1; i++)
			{
				var ci = vl1[i];
				yl1[ci] = i;
			}

			// Find horizontal lines (same y-position)
			for (var i = 0; i < valid0; i++)
			{
				// The index of the component in x-order
				var ci = hl0[i];
				var count = regions0[Algorithms.GetComponentCountOffset(ci, componentSizeLimit)];
				if (count < 40)
					continue;

				var color = regions0[Algorithms.GetComponentColorOffset(ci, componentSizeLimit)];
				var x0 = regions0[Algorithms.GetComponentMinXOffset(ci, componentSizeLimit)];
				var x1 = regions0[Algorithms.GetComponentMaxXOffset(ci, componentSizeLimit)];
				var y0 = regions0[Algorithms.GetComponentMinXOffset(ci, componentSizeLimit)];
				var y1 = regions0[Algorithms.GetComponentMaxXOffset(ci, componentSizeLimit)];
				var cw = x1 - x0;
				var ch = y1 - y0;

				// The x0-order position of the component
				var xi = xl0[ci];

				// Find all other component within a certain distance in horizontal direction
				var dim = Math.Max(Math.Max(cw, ch), 10);
				var nx0 = Math.Max(0, x0 - dim * 2);
				var nx1 = Math.Min(width - 1, x1 + dim * 2);
				var ny0 = Math.Max(0, y0 - dim);
				var ny1 = Math.Min(height - 1, y1 + dim);

				var near = rtree0.Search(new Envelope(nx0, ny0, nx1, ny1));

			}
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
			var regionIndex = new int[height * width];
			var regions = new int[componentLimit * (Algorithms.ComponentItemsOffset + componentSizeLimit)];
			var regiounCount = Algorithms.ComponentAnalysis(
				width, height, source, swt, components, regionIndex, regions, componentLimit, componentSizeLimit, sourceChannels: 1);

			regiounCount.Should().Be(3);
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
