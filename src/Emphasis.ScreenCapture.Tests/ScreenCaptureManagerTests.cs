using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using static Emphasis.ScreenCapture.Tests.TestHelper;

namespace Emphasis.ScreenCapture.Tests
{
	[NonParallelizable]
	public class ScreenCaptureManagerTests
	{
		[Test]
		public void GetScreens()
		{
			var manager = new ScreenCaptureManager();
			var screens = manager.GetScreens();

			screens.Should().NotBeEmpty();
			foreach (var screen in screens)
			{
				screen.AdapterId.Should().NotBe(0);
				screen.OutputName.Should().NotBeNullOrEmpty();

				Console.WriteLine($"{screen.AdapterId} {screen.OutputName}");
			}
		}

		[Test]
		public async Task Capture_Bitmap()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();

			using var capture = await manager.Capture(screen);
			using var bitmap = await capture.ToBitmap();

			Run(bitmap, "capture");
		}

		[Test]
		public async Task Multiple_Capture_should_throw()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();
			
			var capture1 = await manager.Capture(screen);

			Func<Task> act = () => manager.Capture(screen);

			act.Should().Throw<ScreenCaptureException>();
		}

		[Test]
		public async Task Multiple_CaptureStream_should_throw()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();

			var stream1 = manager.CaptureStream(screen);

			var enumerator = stream1.GetAsyncEnumerator();

			Exception ex = null;
			try
			{
				var capture1 = await enumerator.MoveNextAsync();
				var capture2 = await enumerator.MoveNextAsync();
			}
			catch (ScreenCaptureException sce)
			{
				ex = sce;
			}

			ex.Should().BeOfType<ScreenCaptureException>();
		}
		
		[Test]
		public async Task Capture_benchmark()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();

			var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			var sw = Stopwatch.StartNew();

			var count = 0;
			while (!tcs.IsCancellationRequested)
			{
				using var capture = await manager.Capture(screen, cancellationToken: tcs.Token);
				count++;
			}

			sw.Stop();

			Console.WriteLine($"Count: {count}");
			Console.WriteLine($"Elapsed: {sw.Elapsed}");
			Console.WriteLine($"Average: {sw.Elapsed / count}");
			Console.WriteLine($"FPS: {count / sw.Elapsed.TotalSeconds}");
		}

		[Test]
		public async Task CaptureStream_benchmark()
		{
			var manager = new ScreenCaptureManager();
			var screen = manager.GetScreens().FirstOrDefault();

			var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			var sw = Stopwatch.StartNew();
			
			var count = 0;
			await foreach (var capture in manager.CaptureStream(screen, cancellationToken: tcs.Token))
			{
				capture.Dispose();
				count++;
			}

			sw.Stop();

			Console.WriteLine($"Count: {count}");
			Console.WriteLine($"Elapsed: {sw.Elapsed}");
			Console.WriteLine($"Average: {sw.Elapsed / count}");
			Console.WriteLine($"FPS: {count/sw.Elapsed.TotalSeconds}");
		}
	}
}