using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Emphasis.ComputerVision.Tests
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			var benchmarks = BenchmarkSwitcher
				.FromAssembly(typeof(Program).Assembly)
				.Run();

			
		}
	}
}
