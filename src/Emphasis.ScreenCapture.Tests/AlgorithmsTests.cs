using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emphasis.ComputerVision;
using Emphasis.ComputerVision.Primitives;
using Emphasis.OpenCL.Extensions;
using Emphasis.ScreenCapture.Helpers;
using FluentAssertions;
using FluentAssertions.Extensions;
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
			UnoptimizedAlgorithms.Gauss(width, height, source, result);

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

			UnoptimizedAlgorithms.Sobel3(width, height, source, dx, dy, gradient, angle, neighbors);

			Run("sample00.png");
			gradient.RunAs(width, height, 1, "sobel_gradient.png");

			dx.RunAs(width, height, 1, "sobel_dx.png");
			dy.RunAs(width, height, 1, "sobel_dy.png");

			//source.RunAsText(width, height, 4, "sample00.txt");
			//gradient.RunAsText(width, height, 1, "sobel_gradient.txt");
		}

		[Test]
		public async Task GrayscaleTest()
		{
			var sourceBitmap = Samples.sample13;

			Run("sample13.png");

			var source = sourceBitmap.ToBytes();

			var w = sourceBitmap.Width;
			var h = sourceBitmap.Height;

			var result = new byte[h * w];
			var input = new Matrix<byte>(w, h, 4, source);
			var output = new Matrix<byte>(w, h, 1, result);

			var n = 2000;
			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < n; i++)
			{
				await Emphasis.ComputerVision.Core.Algorithms.Grayscale(input, output);
			}
			sw.Stop();
			Console.WriteLine(sw.Elapsed.TotalMicroseconds() / n);

			result.RunAs(w, h, 1, "grayscale.png");
		}

		[Test]
		public void Grayscale_Test()
		{
			var sourceBitmap = Samples.sample13;

			Run("sample13.png");

			var source = sourceBitmap.ToBytes();
			
			var w = sourceBitmap.Width;
			var h = sourceBitmap.Height;
			var n = w * h;

			var grayscale = new byte[n];

			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < 1; i++)
			{
				UnoptimizedAlgorithms.Grayscale(w, h, source, grayscale);
			}
			sw.Stop();
			Console.WriteLine(sw.ElapsedMilliseconds);

			grayscale.RunAs(w, h, 1, "grayscale.png");
		}

		[Test]
		public void Background_Test()
		{
			var sourceBitmap = Samples.sample03;

			Run("sample03.png");

			var source = sourceBitmap.ToBytes();

			var channels = 4;

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;
			var n = width * height;

			var grayscale = new byte[n];
			UnoptimizedAlgorithms.Grayscale(width,height, source, grayscale);

			var background = new byte[n * channels];
			var backgroundSize = 7;
			UnoptimizedAlgorithms.Background(width, height, source, channels, grayscale, background, backgroundSize);

			grayscale.RunAs(width, height, 1, "grayscale.png");
			grayscale.RunAsText(width, height, 1, "grayscale.txt");

			source.RunAsText(width, height, channels, "source.txt");

			background.RunAs(width, height, channels, "background.png");
			background.RunAsText(width, height, channels, "background.txt");

			var grayscaleBg = new byte[n];
			UnoptimizedAlgorithms.GrayscaleEq(width, height, background, grayscaleBg);

			grayscaleBg.RunAs(width, height, 1, "grayscaleBg.png");
			grayscaleBg.RunAsText(width, height, 1, "grayscaleBg.txt");
		}

		[Test]
		public void BoxBlur_Test()
		{
			var sourceBitmap = Samples.sample03;

			Run("sample03.png");

			var source = sourceBitmap.ToBytes();

			var channels = 4;

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;
			var n = width * height;

			var linePrefixSums = new int[n * channels];
			UnoptimizedAlgorithms.LinePrefixSum(width, height, source, channels, linePrefixSums);

			var box = new byte[n * channels];
			var boxSize = 5;
			UnoptimizedAlgorithms.BoxBlur(width, height, linePrefixSums, channels, box, boxSize);

			source.RunAsText(width, height, channels, "source.txt");

			box.RunAs(width, height, channels, $"box{boxSize}.png");
			box.RunAsText(width, height, channels, $"box{boxSize}.txt");
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
			UnoptimizedAlgorithms.Enlarge2(width, height, source, large, channels);

			large.RunAs(width * 2, height * 2, channels, "large.png");
		}

		[Test]
		public void NonMaximumSuppression_Test()
		{
			var sourceBitmap = Samples.sample03;

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

			var n = height * width;
			var grayscaleEq = new byte[n];
			var background = new byte[n * channels];

			var bgWindowSize = 7;
			UnoptimizedAlgorithms.GrayscaleEq(width, height, source, grayscaleEq);
			UnoptimizedAlgorithms.Background(width, height, source, channels, grayscaleEq, background, bgWindowSize);

			source.RunAs(width, height, channels, $"source.png");
			//background.RunAs(width, height, channels, $"background{bgWindowSize}.png");

			byte[] large = null;
			if (useLarge)
			{
				large = new byte[n * 4 * channels];
				UnoptimizedAlgorithms.Enlarge2(width, height, source, large, channels);

				width *= 2;
				height *= 2;
				n = height * width;
			}
			var src = useLarge ? large : source;

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

			UnoptimizedAlgorithms.Grayscale(width, height, src, grayscale);
			UnoptimizedAlgorithms.Gauss(width, height, src, gauss);
			UnoptimizedAlgorithms.Sobel3(width, height, gauss,  dx, dy, gradient, angle, neighbors);
			UnoptimizedAlgorithms.NonMaximumSuppression(width, height, gradient, angle, neighbors, nms, cmp0, cmp1);
			UnoptimizedAlgorithms.StrokeWidthTransform(width, height, src, gradient, nms, angle, dx, dy, swt0, swt1,
				sourceChannels: 4,
				rayLength: 30,
				colorDifference: swtEdgeColorTolerance,
				useStrokeColor: swtEdgeOnColorChange);

			var coloring0 = new int[height * width];
			var coloring1 = new int[height * width];

			UnoptimizedAlgorithms.PrepareComponents(swt0, coloring0);
			UnoptimizedAlgorithms.PrepareComponents(swt1, coloring1);

			var colorRounds0 = UnoptimizedAlgorithms.ColorComponentsFixedPointBackPropagation(
				width, height, swt0, coloring0);
			var colorRounds1 = UnoptimizedAlgorithms.ColorComponentsFixedPointBackPropagation(
				width, height, swt1, coloring1);

			var componentIndexByColoring0 = new int[n];
			var componentIndexByColoring1 = new int[n];

			var componentsLimit = 100000;
			var componentSizeLimit = 1024;
			var cn = componentsLimit * componentSizeLimit;

			var componentItems0 = new int[cn];
			var componentItems1 = new int[cn];

			var componentSwtItems0 = new int[cn];
			var componentSwtItems1 = new int[cn];

			var components0 = new Component[componentsLimit];
			var components1 = new Component[componentsLimit];

			var regionCount0 = UnoptimizedAlgorithms.ComponentAnalysis(
				width, height, src, swt0, coloring0, componentIndexByColoring0, componentItems0, componentSwtItems0, components0, componentsLimit, componentSizeLimit, sourceChannels: channels);
			var regionCount1 = UnoptimizedAlgorithms.ComponentAnalysis(
				width, height, src, swt1, coloring1, componentIndexByColoring1, componentItems1, componentSwtItems0, components1, componentsLimit, componentSizeLimit, sourceChannels: channels);

			UnoptimizedAlgorithms.ColorComponentsFixedPointByColorSimilarity(
				width, height, swt0, coloring0, src, background, channels, componentIndexByColoring0, components0, 30, useLarge);
			UnoptimizedAlgorithms.ColorComponentsFixedPointByColorSimilarity(
				width, height, swt1, coloring1, src, background, channels, componentIndexByColoring1, components1, 30, useLarge);

			// Filter components 1st pass
			//var valid0 = Algorithms.PassiveFilter(regionCount0, componentList0);
			//var valid1 = Algorithms.PassiveFilter(regionCount1, componentList1);

			//var invalid0 = regionCount0 - valid0;
			//var invalid1 = regionCount1 - valid1;

			//Console.WriteLine($"Components BoW: {valid0}/{invalid0}");
			//Console.WriteLine($"Components WoB: {valid1}/{invalid1}");

			//var rtree0 = Algorithms.ComponentRBush(regionCount0, componentList0);
			//var rtree1 = Algorithms.ComponentRBush(regionCount1, componentList1);

			//Algorithms.MergeComponents(width, height, regionCount0, componentList0, rtree0);
			//Algorithms.MergeComponents(width, height, regionCount1, componentList1, rtree1);

			//Algorithms.RemoveBoxes(width, height, regionCount0, componentList0, rtree0);
			//Algorithms.RemoveBoxes(width, height, regionCount1, componentList1, rtree1);

			if (useLarge)
			{
				large.RunAs(width, height, channels, "large.png");
				//large.RunAsText(width, height, channels, "large.txt");
			}

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

			coloring0.RunAs(width, height, 1, "cc0.png");
			coloring0.ReplaceGreaterOrEquals(n, 0).RunAsText(width, height, 1, "cc0.txt");

			coloring1.RunAs(width, height, 1, "cc1.png");
			coloring1.ReplaceGreaterOrEquals(n, 0).RunAsText(width, height, 1, "cc1.txt");

			var text0 = new int[n];
			var text1 = new int[n];
			for (var i = 0; i < n; i++)
			{
				var color = coloring0[i];
				if (color >= n)
				{
					text0[i] = 255;
					continue;
				}

				var ci = componentIndexByColoring0[color];
				if (color >= n || ci == -1)
				{
					text0[i] = 255;
					continue;
				}
				ref var c = ref components0[ci];
				if (!c.IsValid())
					text0[i] = c.Validity;
			}

			for (var i = 0; i < n; i++)
			{
				var color = coloring1[i];
				if (color >= n)
				{
					text1[i] = 255;
					continue;
				}

				var ci = componentIndexByColoring1[color];
				if (color >= n || ci == -1)
				{
					text1[i] = 255;
					continue;
				}
				ref var c = ref components1[ci];
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

			var coloring = new int [height * width];
			
			var rounds = UnoptimizedAlgorithms.ColorComponentsFixedPoint(
				width, height, swt, coloring);

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

			UnoptimizedAlgorithms.Dump(result, width, height);
			UnoptimizedAlgorithms.Dump(coloring, width, height);

			coloring.Should().Equal(result);
			Console.WriteLine($"Rounds: {rounds}");

			var componentLimit = 1024;
			var componentSizeLimit = 6;
			var cn = componentLimit * componentSizeLimit;
			var regionIndex = new int[height * width];
			var regions = new int[cn];
			var regionSwt = new int[cn];
			var components = new Component[componentLimit];

			var regionCount = UnoptimizedAlgorithms.ComponentAnalysis(
				width, height, source, swt, coloring, regionIndex, regions, regionSwt, components, componentLimit, componentSizeLimit, sourceChannels: 1);

			regionCount.Should().Be(3);
			regionIndex[6].Should().Be(0);
			regionIndex[3].Should().Be(1);
			regionIndex[56].Should().Be(2);

			components[0].Coloring.Should().Be(6);
			components[0].Size.Should().Be(25);
			components[0].SwtSize.Should().Be(5);
			components[0].SwtSum.Should().Be(6);
			components[0].X0.Should().Be(6);
			components[0].X1.Should().Be(13);
			components[0].Y0.Should().Be(0);
			components[0].Y1.Should().Be(1);

			regions.AsSpan(0, componentSizeLimit).ToArray()
				.Should().Equal(6, 11, 12, 13, 20, 25);
			regionSwt.AsSpan(0, componentSizeLimit).ToArray()
				.Should().Equal(1, 1, 1, 1, 1, 1);

			components[1].Coloring.Should().Be(3);
			components[1].Size.Should().Be(4); // count -2
			components[1].SwtSize.Should().Be(4); // swt count -2
			components[1].SwtSum.Should().Be(11); // sum
			components[1].X0.Should().Be(2); // min x
			components[1].X1.Should().Be(5); // max x
			components[1].Y0.Should().Be(1); // min y
			components[1].Y1.Should().Be(3); // max y

			regions.AsSpan(2 * componentSizeLimit, componentSizeLimit).ToArray()
				.Should().Equal(3, 16, 17, 18, 32, 47);
			regionSwt.AsSpan(2 * componentSizeLimit, componentSizeLimit).ToArray()
				.Should().Equal(2, 2, 1, 4, 2, 0);
		}

		[Test]
		public void ConnectedComponentsAnalysis()
		{
			var max = int.MaxValue;
			var width = 10;
			var height = 10;
			var n = height * width;

			var swt = new int[]
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

			var coloring0 = new int[height * width];
			var coloring1 = new int[height * width];
			var coloring2 = new int[height * width];

			UnoptimizedAlgorithms.PrepareComponents(swt, coloring0);
			UnoptimizedAlgorithms.PrepareComponents(swt, coloring1);
			UnoptimizedAlgorithms.PrepareComponents(swt, coloring2);

			var roundsWatershed = UnoptimizedAlgorithms.ColorComponentsWatershed(
				width, height, swt, coloring0);
			var roundsFixedPoint = UnoptimizedAlgorithms.ColorComponentsFixedPoint(
				width, height, swt, coloring1);
			var roundsBackPropagation = UnoptimizedAlgorithms.ColorComponentsFixedPointBackPropagation(
				width, height, swt, coloring2);

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
				if (coloring0[i] >= n)
					coloring0[i] = r0;
				if (coloring1[i] >= n)
					coloring1[i] = r0;
				if (coloring2[i] >= n)
					coloring2[i] = r0;
			}

			coloring0.Should().Equal(result);
			coloring1.Should().Equal(result);
			coloring2.Should().Equal(result);

			Console.WriteLine($"Rounds Watershed:       {roundsWatershed}");
			Console.WriteLine($"Rounds FixedPoint:      {roundsFixedPoint}");
			Console.WriteLine($"Rounds BackPropagation: {roundsBackPropagation}");

			roundsFixedPoint.Should().BeLessThan(roundsWatershed);
			roundsBackPropagation.Should().BeLessThan(roundsFixedPoint);
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
				var n = UnoptimizedAlgorithms.GradientNeighbors(i);

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
