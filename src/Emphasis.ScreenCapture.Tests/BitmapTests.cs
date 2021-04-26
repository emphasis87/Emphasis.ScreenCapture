using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Emphasis.ScreenCapture.Tests
{
	[NonParallelizable]
	public class BitmapTests
	{
		[Test]
		public async Task Capture_benchmark()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();

			var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			
			using var capture = await manager.Capture(screen, cancellationToken: tcs.Token);

			var sw = Stopwatch.StartNew();
			sw.Start();
			var n = 1000;
			for (var i = 0; i < n; i++)
			{
				var bitmap = await capture.ToBitmap();
				bitmap.Dispose();
			}
			sw.Stop();

			Console.WriteLine($"Average: {(int)(sw.Elapsed.TotalMicroseconds()/n)} us");
		}
	}
}
