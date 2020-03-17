using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

			Run(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sample02.png")));

			var resultBitmap = result.ToBitmap(width, height, 4);
			var resultPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "gauss.png"));
			resultBitmap.Save(resultPath);

			Run(resultPath);
		}

		[Test]
		public void Sobel_Test()
		{
			var sourceBitmap = Samples.sample02;
			var source = sourceBitmap.ToBytes();

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var gradient = new byte[source.Length];
			var direction = new byte[source.Length];
			Algorithms.Sobel(width, height, source, gradient, direction);

			Run(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sample02.png")));

			var gradientBitmap = gradient.ToBitmap(width, height, 1);
			var gradientPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sobel_gradient.png"));
			gradientBitmap.Save(gradientPath);

			Run(gradientPath);
		}
	}
}
