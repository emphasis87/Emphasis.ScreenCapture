using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using NUnit.Framework;

namespace Emphasis.ScreenCapture.Tests
{
	public class CameraCaptureTests
	{
		[Test]
		public async Task Can_capture_camera()
		{
			using var capture = new VideoCapture(captureApi: VideoCapture.API.DShow);
			capture.ImageGrabbed += CaptureOnImageGrabbed;
			capture.Start();

			for (int i = 0; i < 10; i++)
			{
				capture.Grab();
				await Task.Delay(1000);
			}
		}

		private void CaptureOnImageGrabbed(object? sender, EventArgs e)
		{
			if (sender is VideoCapture capture)
			{
				
			}
		}
	}
}
 