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
			private byte[] _grayscaleEq;
			private byte[] _grayscaleEq2;
			private float[] _dx;
			private float[] _dy;
			private float[] _gradient;
			private float[] _angle;
			private byte[] _neighbours;
			private byte[] _large;
			private byte[] _background;
			private byte[] _background2;

			[Params(1920)]
			public int Width;

			[Params(1200)]
			public int Height;

			private int N;
			private int M;
			private int W2;
			private int H2;
			private int N2;
			private int M2;

			[GlobalSetup]
			public void Setup()
			{
				N = Height * Width;
				M = N * channels;
				W2 = Width * 2;
				H2 = Height * 2;
				N2 = W2 * H2;
				M2 = N2 * channels;

				_source = new byte[M];
				_grayscale = new byte[N];
				_grayscaleEq = new byte[N];
				_grayscaleEq = new byte[N2];
				_dx = new float[N];
				_dy = new float[N];
				_gradient = new float[N];
				_angle = new float[N];
				_neighbours = new byte[N * 5];
				_large = new byte[M2];
				_background = new byte[M];
				_background2 = new byte[M2];
			}

			[Benchmark]
			public void Grayscale()
			{
				UnoptimizedAlgorithms.Grayscale(Width, Height, _source, _grayscale);
			}

			[Benchmark]
			public void GrayscaleEq()
			{
				UnoptimizedAlgorithms.GrayscaleEq(Width, Height, _source, _grayscale);
			}

			[Benchmark]
			public void Sobel3()
			{
				UnoptimizedAlgorithms.Sobel3(Width, Height, _source, _dx, _dy, _gradient, _angle, _neighbours, channels);
			}

			[Benchmark]
			public void Enlarge2()
			{
				UnoptimizedAlgorithms.Enlarge2(Width, Height, _source, _large, channels);
			}

			[Benchmark]
			public void Background5()
			{
				UnoptimizedAlgorithms.Background(Width, Height, _source, channels, _grayscaleEq, _background, 5);
			}

			[Benchmark]
			public void BackgroundLarge7()
			{
				UnoptimizedAlgorithms.Background(W2, H2, _large, channels, _grayscaleEq, _background2, 7);
			}
		}
	}
}
