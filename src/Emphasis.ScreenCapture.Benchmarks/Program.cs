using System;
using BenchmarkDotNet.Running;

namespace Emphasis.ScreenCapture.Benchmarks
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}
}
