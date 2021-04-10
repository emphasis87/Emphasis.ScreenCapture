using System;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emphasis.ScreenCapture.Helpers;
using FluentAssertions.Extensions;
using NUnit.Framework;
using static Emphasis.ScreenCapture.Helpers.DebugHelper;

namespace Emphasis.ScreenCapture.Tests
{
	public class EmguTests
	{
		[Test]
		public void Grayscale()
		{
			var sourceBitmap = Samples.sample13;

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
			
			var n = 10000;
			var sw = new Stopwatch();
			using (var gray = new UMat())
			{
				CvInvoke.CvtColor(image, gray, ColorConversion.Bgra2Gray);
				sw.Start();
				for (var i = 0; i < n; i++)
				{
					CvInvoke.CvtColor(image, gray, ColorConversion.Bgra2Gray);
				}
			}
			sw.Stop();
			Console.WriteLine(sw.Elapsed.TotalMicroseconds() / n);
		}

		[Test]
		public void Canny()
		{
			var sourceBitmap = Samples.sample13;

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
			
			var n = 2000;
			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < n; i++)
			{
				image.Canny(50, 30, 5, false);
			}
			sw.Stop();
			Console.WriteLine(sw.Elapsed.TotalMicroseconds() / n);
			
			var result = new float[height * width];
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					result[d] = canny.Data[y, x, 0];
				}
			}

			Run("sample13.png");
			result.RunAs(width, height, 1, "canny.png");
		}

		[Test]
		public void MSER()
		{
			var sourceBitmap = Samples.sample03;

			var source = sourceBitmap.ToBytes();

			var w = sourceBitmap.Width;
			var h = sourceBitmap.Height;

			var data = new byte[h, w, 4];
			for (var y = 0; y < h; y++)
			{
				for (var x = 0; x < w; x++)
				{
					var d = y * (w * 4) + x * 4;
					for (var c = 0; c < 4; c++)
					{
						data[y, x, c] = source[d + c];
					}
				}
			}
			
			var image = new Image<Bgra, byte>(data);

			MSERDetector detector = new MSERDetector(
				minArea:5);
			var msers = new VectorOfVectorOfPoint();
			var bboxes = new VectorOfRect();
			detector.DetectRegions(image, msers, bboxes);

			var result = new byte[w * h];
			foreach (var mser in msers.ToArrayOfArray())
			{
				foreach (var point in mser)
				{
					result[point.Y * w + point.X] = 255;
				}
			}
			foreach (var bbox in bboxes.ToArray())
			{
				
			}


			Run("sample03.png");
			result.RunAs(w, h, 1, "mser.png");
		}
	}
}
