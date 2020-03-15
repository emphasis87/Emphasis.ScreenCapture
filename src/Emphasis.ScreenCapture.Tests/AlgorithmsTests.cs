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
		public void GaussTest()
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
	}
}
