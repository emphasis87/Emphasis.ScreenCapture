using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
	}
}