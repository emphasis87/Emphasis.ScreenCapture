using System;
using System.Diagnostics;
using NUnit.Framework;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Tests
{
	public class DxgiTests
	{
		[Test]
		public void Get_all_outputs_benchmark()
		{
			// Create DXGI Factory1
			var factory = new Factory1();

			var sw = Stopwatch.StartNew();
			for (var i = 0; i < 1000; i++)
			{
				var adapters = factory.Adapters;
				foreach (var adapter in adapters)
				{
					var outputs = adapter.Outputs;
					foreach (var output in outputs)
					{
						
					}
				}
			}
			sw.Stop();

			var microseconds = (sw.ElapsedTicks/1000) / (TimeSpan.TicksPerMillisecond / 1000);
			Console.WriteLine($"Collecting outputs: {microseconds}microseconds");
		}
	}
}
