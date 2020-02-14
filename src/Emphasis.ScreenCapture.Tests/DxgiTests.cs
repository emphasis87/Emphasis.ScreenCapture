using System;
using System.Diagnostics;
using NUnit.Framework;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Tests
{
	public class DxgiTests
	{
		private const int Count = 10000;

		/// <summary>
		/// Retrieving the list of outputs is quite cheap (&lt;0.1ms).
		/// </summary>
		[Test]
		public void Get_all_outputs_benchmark()
		{
			var factory = new Factory1();

			var sw = Stopwatch.StartNew();
			for (var i = 0; i < Count; i++)
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

			var microseconds = (sw.ElapsedTicks/Count) / (TimeSpan.TicksPerMillisecond / 1000);
			Console.WriteLine($"Collecting outputs: {microseconds}microseconds");
		}
	}
}
