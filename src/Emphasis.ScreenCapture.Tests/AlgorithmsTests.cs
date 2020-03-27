using System;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emphasis.ComputerVision;
using Emphasis.ScreenCapture.Helpers;
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

			var gradient = new float[source.Length];
			var angle = new float[source.Length];
			//var direction = new byte[source.Length];
			Algorithms.Sobel(width, height, source, gradient, angle);

			Run("sample00.png");
			gradient.RunAs(width, height, 1, "sobel_gradient.png");

			source.RunAsText(width, height, 4, "sample00.txt");
			gradient.RunAsText(width, height, 1, "sobel_gradient.txt");
			//direction.RunAsText(width, height, 1, "sobel_direction.txt");
		}

		[Test]
		public async Task NonMaximumSuppression_Test()
		{
			var sourceBitmap = Samples.sample02;

			var source = sourceBitmap.ToBytes();
			var gauss = new byte[source.Length];

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var grayscale = new byte[height * width];

			var gradient = new float[height * width];
			var angle = new float[height * width];
			var neighbors = new byte[height * width * 4];
			var gradientNms = new float[height * width];
			var cmp1 = new float[height * width];
			var cmp2 = new float[height * width];

			Algorithms.Grayscale(width,height, source, grayscale);

			Algorithms.Gauss(width, height, source, gauss);

			Algorithms.Sobel(width, height, gauss, gradient, angle);

			Algorithms.NonMaximumSuppression(width, height, gradient, angle, neighbors, gradientNms, cmp1, cmp2);

			Run("sample02.png");

			grayscale.RunAs(width, height, 1, "grayscale.png");
			gradient.RunAs(width, height, 1, "sobel_gradient.png");
			gradientNms.RunAs(width, height, 1, "sobel_gradient_nms.png");

			//source.RunAsText(width, height, 4, "sample00.txt");
			//await Task.Delay(100);

			grayscale.RunAsText(width, height, 1, "grayscale.txt");
			await Task.Delay(100);

			angle.RunAsText(width, height, 1, "sobel_angle.txt");
			await Task.Delay(100);

			//direction.RunAsText(width, height, 1, "sobel_direction.txt");
			//await Task.Delay(100);

			gradient.RunAsText(width, height, 1, "sobel_gradient.txt");
			await Task.Delay(100);

			//gradientNms.RunAsText(width, height, 1, "sobel_gradient_nms.txt");
			//await Task.Delay(100);

			//cmp1.RunAsText(width, height, 1, "cmp1.txt");
			//await Task.Delay(100);

			//cmp2.RunAsText(width, height, 1, "cmp2.txt");
			//await Task.Delay(100);

			//var data = new byte[height, width, 4];
			//for (var y = 0; y < height; y++)
			//{
			//	for (var x = 0; x < width; x++)
			//	{
			//		var d = y * (width * 4) + x * 4;
			//		for (var c = 0; c < 4; c++)
			//		{
			//			data[y, x, c] = source[d + c];
			//		}
			//	}
			//}

			//var image = new Image<Bgra, byte>(data);
			//var canny = image.Canny(50, 20);

			//var result = new byte[height * width];
			//for (var y = 0; y < height; y++)
			//{
			//	for (var x = 0; x < width; x++)
			//	{
			//		var d = y * width + x;
			//		result[d] = canny.Data[y, x, 0];
			//	}
			//}

			//result.RunAs(width, height, 1, "canny.png");
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
	}
}
