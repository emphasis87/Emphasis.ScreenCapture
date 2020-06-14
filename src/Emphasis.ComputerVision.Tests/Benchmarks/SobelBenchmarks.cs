using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Emphasis.ComputerVision.Tests.Benchmarks
{
	[SimpleJob()]
	public class SobelBenchmarks
	{
		[Params(1920)]
		public int Width;

		[Params(1200)]
		public int Height;
	}
}
