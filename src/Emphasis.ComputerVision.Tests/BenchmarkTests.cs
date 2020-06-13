using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Runtime;

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
			private float[] _dx;
			private float[] _dy;
			private float[] _gradient;
			private float[] _angle;
			private byte[] _neighbours;

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
				_grayscale = new byte[N];
				_dx = new float[N];
				_dy = new float[N];
				_gradient = new float[N];
				_angle = new float[N];
				_neighbours = new byte[N * 5];
			}

			[Benchmark]
			public void Grayscale()
			{
				Algorithms.Grayscale(Width, Height, _source, _grayscale);
			}

			[Benchmark]
			public void Sobel3()
			{
				Algorithms.Sobel3(Width, Height, _source, _dx, _dy, _gradient, _angle, _neighbours, channels);
			}
		}
	}
}
