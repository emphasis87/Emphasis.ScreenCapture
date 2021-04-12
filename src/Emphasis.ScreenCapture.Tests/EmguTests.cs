using System;
using System.Diagnostics;
using System.Drawing;
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
		public void Grayscale_UMat()
		{
			var sourceBitmap = Samples.sample13;

			var w = sourceBitmap.Width;
			var h = sourceBitmap.Height;

			using var src = new UMat();

			var srcMat = sourceBitmap.ToMat();
			srcMat.CopyTo(src);

			var n = 10000;
			var sw = new Stopwatch();
			using var gray = new UMat();

			CvInvoke.CvtColor(src, gray, ColorConversion.Bgra2Gray);
			sw.Start();
			for (var i = 0; i < n; i++)
			{
				CvInvoke.CvtColor(src, gray, ColorConversion.Bgra2Gray);
			}

			sw.Stop();
			Console.WriteLine(sw.Elapsed.TotalMicroseconds() / n);

			gray.Bytes.RunAs(w, h, 1, "gray.png");
		}

		[Test]
		public void Canny()
		{
			var sourceBitmap = Samples.sample13;

			var w = sourceBitmap.Width;
			var h = sourceBitmap.Height;

			using var src = new UMat();

			var srcMat = sourceBitmap.ToMat();
			srcMat.CopyTo(src);

			using var dest1 = new UMat();
			using var dest2 = new UMat();
			using var gray = new UMat();

			CvInvoke.CvtColor(src, gray, ColorConversion.Bgra2Gray);
			CvInvoke.Canny(gray, dest1, 100, 40);
			
			var n = 10000;
			var sw = new Stopwatch();
			sw.Start();

			var exCount = 0;
			for (var i = 0; i < n; i++)
			{
				try
				{
					CvInvoke.Canny(gray, dest1, 100, 40);
				}
				catch (Exception ex)
				{
					exCount++;
				}
			}

			sw.Stop();

			Console.WriteLine((int)(sw.Elapsed.TotalMicroseconds() / n));
			Console.WriteLine($"Exceptions {exCount}");

			Run("sample13.png");
			dest1.Bytes.RunAs(w, h, 1, "canny.png");

			
		}

		[Test]
		public void Resize()
		{
			var sourceBitmap = Samples.sample13;

			var w = sourceBitmap.Width;
			var h = sourceBitmap.Height;

			using var src = new UMat();

			var srcMat = sourceBitmap.ToMat();
			srcMat.CopyTo(src);

			using var dest = new UMat();
			using var gray = new UMat();

			CvInvoke.CvtColor(src, gray, ColorConversion.Bgra2Gray);
			CvInvoke.Resize(gray, dest, new Size(w * 2, h * 2));

			var n = 3000;
			var sw = new Stopwatch();
			sw.Start();

			for (var i = 0; i < n; i++)
			{
				CvInvoke.Resize(gray, dest, new Size(w * 2, h * 2));
			}

			sw.Stop();

			Console.WriteLine((int)(sw.Elapsed.TotalMicroseconds() / n));

			Run("sample13.png");
			
			dest.Save("resize.png");
			Run("resize.png");
		}

		[Test]
		public void MSER()
		{
			var sourceBitmap = Samples.sample13;
			
			var w = sourceBitmap.Width;
			var h = sourceBitmap.Height;

			using var src = new UMat();

			using var srcMat = sourceBitmap.ToMat();
			srcMat.CopyTo(src);

			using var gray = new UMat();
			
			CvInvoke.CvtColor(src, gray, ColorConversion.Bgra2Gray);
			
			using var detector = new MSERDetector(
				minArea:5, maxArea:80, edgeBlurSize: 5);
			using var msers = new VectorOfVectorOfPoint();
			using var bboxes = new VectorOfRect();

			detector.DetectRegions(gray, msers, bboxes);
			
			var sw = new Stopwatch();
			sw.Start();

			var n = 100;
			for(var i = 0; i < n; i++)
				detector.DetectRegions(gray, msers, bboxes);

			sw.Stop();

			Console.WriteLine((int)(sw.Elapsed.TotalMicroseconds() / n));

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


			Run("sample13.png");
			result.RunAs(w, h, 1, "mser.png");
		}
	}
}
