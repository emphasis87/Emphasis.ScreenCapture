using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emphasis.ScreenCapture.Windows.Dxgi;
using FluentAssertions;
using NUnit.Framework;

namespace Emphasis.ScreenCapture.Tests
{
	public class ScreenCaptureManagerTests
	{
		[Test]
		public void Can_GetScreens()
		{
			var manager = new ScreenCaptureManager();
			var screens = manager.GetScreens();

			screens.Should().NotBeEmpty();
			foreach (var screen in screens)
			{
				screen.AdapterId.Should().NotBe(0);
				screen.OutputName.Should().NotBeNullOrEmpty();
			}
		}

		[Test]
		public async Task Can_GetScreenChanges()
		{
			var manager = new ScreenCaptureManager();

			var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			var screensList = new List<Screen[]>();
			await foreach (var screens in manager.GetScreenChanges(cancellationToken: tcs.Token))
			{
				screensList.Add(screens);
			}

			screensList.Should().HaveCount(1);
			foreach (var screens in screensList)
			{
				screens.Should().NotBeEmpty();
				foreach (var screen in screens)
				{
					screen.AdapterId.Should().NotBe(0);
					screen.OutputName.Should().NotBeNullOrEmpty();
				}
			}
		}

		[Test]
		public async Task Can_CaptureAll()
		{
			var manager = new ScreenCaptureManager();
			
			var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			var captureList = new List<ScreenCapture>();
			await foreach (var capture in manager.CaptureAll(cancellationToken: tcs.Token))
			{
				captureList.Add(capture);
				await Task.Delay(TimeSpan.FromSeconds(1));
			}

			captureList.Should().HaveCount(10);
		}

		[Test]
		public async Task CaptureAll()
		{
			var manager = new ScreenCaptureManager();

			var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(5));

			var count = 0;
			await foreach (var capture in manager.CaptureAll(cancellationToken: tcs.Token))
			{
				if (count++ >= 3)
					return;

				await Task.Delay(TimeSpan.FromSeconds(1));

				if (capture is DxgiScreenCapture dxgiScreenCapture && 
				    capture.Method is IDxgiScreenCaptureMethod dxgiScreenCaptureMethod)
				{
					var bitmap = await dxgiScreenCaptureMethod.ToBitmap(dxgiScreenCapture);
					var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"{count}.bmp"));
					bitmap.Save(path);
					Process.Start(new ProcessStartInfo(path)
					{
						UseShellExecute = true
					});
				}
			}
		}

		[Test]
		public async Task CaptureAll_fps()
		{
			var manager = new ScreenCaptureManager();

			var tcs = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			var sw = Stopwatch.StartNew();
			var count = 0;
			await foreach (var capture in manager.CaptureAll(cancellationToken: tcs.Token))
			{
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