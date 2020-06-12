using System;
using BenchmarkDotNet.Attributes;

namespace Emphasis.ComputerVision.Tests
{
	public class BenchmarkTests
	{
		[SimpleJob]
		public class AlgorithmTests
		{
			private int channels = 4;
			private byte[] _source;
			private byte[] _grayscale;

			[Params(1920)]
			public int Width;

			[Params(1200)]
			public int Height;

			private int N;
			private int M;

			[GlobalSetup]
			public void Setup()
			{
				N = Height * Width;
				M = N * channels;

				_source = new byte[M];
				_grayscale = new byte[M];
			}

			[Benchmark]
			public void Grayscale()
			{
				Algorithms.Grayscale(Width, Height, _source, _grayscale);
			}
		}
	}
}
