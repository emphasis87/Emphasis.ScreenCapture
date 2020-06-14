using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Emphasis.ComputerVision.Tests.Benchmarks
{
	[SimpleJob]
	public class SobelBenchmarks
	{
		[Params(1920)]
		public int Width;

		[Params(1200)]
		public int Height;

		public int Channels = 4;

		private byte[] _source;
		private float[] _dx;
		private float[] _dy;
		private float[] _gradient;
		private float[] _angle;
		private byte[] _neighbours;

		[GlobalSetup]
		public void Setup()
		{
			var n = Width * Height;
			var m = n * Channels;
			_source = new byte[m];
			_dx = new float[n];
			_dy = new float[n];
			_gradient = new float[n];
			_angle = new float[n];
			_neighbours = new byte[n * 5];
		}

		[Benchmark]
		public void Sobel3()
		{
			UnoptimizedAlgorithms.Sobel3(Width, Height, _source, _dx, _dy, _gradient, _angle, _neighbours, Channels);
		}
	}
}
