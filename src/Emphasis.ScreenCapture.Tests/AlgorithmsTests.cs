using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
			var sourceBitmap = Samples.sample00;
			var source = sourceBitmap.ToBytes();

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var gradient = new float[source.Length];
			var angle = new float[source.Length];
			var direction = new byte[source.Length];
			var gradientNms = new float[source.Length];
			var cmp1 = new float[source.Length];
			var cmp2 = new float[source.Length];

			Algorithms.Sobel(width, height, source, gradient, angle);

			for (var i = 0; i < angle.Length; i++)
			{
				var a = angle[i];
				angle[i] = ((a + 2) % 2) * 180;

				direction[i] = Algorithms.ConvertAtan2PiAngleTo8Way(a);
			}

			Algorithms.NonMaximumSuppression(width, height, gradient, direction, gradientNms, cmp1, cmp2);

			Run("sample00.png");

			gradient.RunAs(width, height, 1, "sobel_gradient.png");
			gradientNms.RunAs(width, height, 1, "sobel_gradient_nms.png");

			source.RunAsText(width, height, 4, "sample00.txt");
			await Task.Delay(100);
			
			angle.RunAsText(width, height, 1, "sobel_angle.txt");
			await Task.Delay(100);

			direction.RunAsText(width, height, 1, "sobel_direction.txt");
			await Task.Delay(100);

			gradient.RunAsText(width, height, 1, "sobel_gradient.txt");
			await Task.Delay(100);

			gradientNms.RunAsText(width, height, 1, "sobel_gradient_nms.txt");
			await Task.Delay(100);

			cmp1.RunAsText(width, height, 1, "cmp1.txt");
			await Task.Delay(100);

			cmp2.RunAsText(width, height, 1, "cmp2.txt");
			await Task.Delay(100);
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
