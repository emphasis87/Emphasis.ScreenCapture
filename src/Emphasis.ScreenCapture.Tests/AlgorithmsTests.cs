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

			Run("sample02.png");

			result.RunAs(width, height, 4, "gauss.png");
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

			Run("sample02.png");

			gradient.RunAs(width, height, 1, "sobel_gradient.png");
		}

		[Test]
		public void NonMaximumSuppression_Test()
		{
			var sourceBitmap = Samples.sample02;
			var source = sourceBitmap.ToBytes();

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			var gradient = new byte[source.Length];
			var direction = new byte[source.Length];
			var gradientNms = new byte[source.Length];

			Algorithms.Sobel(width, height, source, gradient, direction);
			Algorithms.NonMaximumSuppression(width, height, gradient, gradientNms);

			Run("sample02.png");

			gradient.RunAs(width, height, 1, "sobel_gradient.png");
			gradientNms.RunAs(width, height, 1, "sobel_gradient_nms.png");
		}
	}
}
