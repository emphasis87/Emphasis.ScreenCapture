using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cloo;
using Emphasis.OpenCL;
using Emphasis.OpenCL.Helpers;
using Emphasis.ScreenCapture.Helpers;
using Emphasis.ScreenCapture.OpenCL;
using Emphasis.TextDetection;
using NUnit.Framework;
using static Emphasis.ScreenCapture.Helpers.DebugHelper;

namespace Emphasis.ScreenCapture.Tests
{
	public class KernelsTests
	{
		[Test]
		public async Task Can_EnqueueGrayscale()
		{
			var manager = new ScreenCaptureManager();
			var dispatcher = new ComputeMemoryDispatcher();

			var screen = manager.GetScreens().First();

			using var capture = await manager.Capture(screen).FirstAsync();
			var width = capture.Width;
			var height = capture.Height;
			var globalWorkSize = new long[] {width, height};

			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var computeManager = new ComputeManager();
			using var kernels = new Kernels(computeManager);
			var context = computeManager.GetContext(device);
			using var image = await dispatcher.Dispatch(capture, context);

			var grayscale = new byte[height * width];
			using var grayscaleBuffer = context.CreateBuffer(grayscale);

			var events = new List<ComputeEventBase>();
			kernels.EnqueueGrayscale(device, globalWorkSize, image, grayscaleBuffer, events);

			await events.WaitForEvents();

			var result = grayscale.ToBitmap(width, height, 1);
			var resultPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "grayscale.png"));
			result.Save(resultPath);

			Run(resultPath);
		}

		[Test]
		public async Task Can_EnqueueThreshold()
		{
			var manager = new ScreenCaptureManager();
			var dispatcher = new ComputeMemoryDispatcher();

			var screen = manager.GetScreens().First();

			using var capture = await manager.Capture(screen).FirstAsync();
			var width = capture.Width;
			var height = capture.Height;
			var globalWorkSize = new long[] { width, height };

			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var computeManager = new ComputeManager();
			using var kernels = new Kernels(computeManager);
			var context = computeManager.GetContext(device);
			using var image = await dispatcher.Dispatch(capture, context);

			var grayscale = new byte[height * width];
			using var grayscaleBuffer = context.CreateBuffer(grayscale);

			var threshold = new byte[height * width];
			using var thresholdBuffer = context.CreateBuffer(threshold);

			var events = new List<ComputeEventBase>();
			kernels.EnqueueGrayscale(device, globalWorkSize, image, grayscaleBuffer, events);
			kernels.EnqueueThreshold(device, grayscaleBuffer, thresholdBuffer, 123, 0, 255, events);

			await events.WaitForEvents();

			var result = threshold.ToBitmap(width, height, 1);
			var resultPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "threshold.png"));
			result.Save(resultPath);

			Run(resultPath);
		}

		[Test]
		public async Task Can_EnqueueNonMaximumSuppression()
		{
			var manager = new ScreenCaptureManager();
			var dispatcher = new ComputeMemoryDispatcher();

			var screen = manager.GetScreens().First();

			using var capture = await manager.Capture(screen).FirstAsync();
			var width = capture.Width;
			var height = capture.Height;
			var globalWorkSize = new long[] { width, height };

			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var computeManager = new ComputeManager();
			using var kernels = new Kernels(computeManager);
			var context = computeManager.GetContext(device);
			using var image = await dispatcher.Dispatch(capture, context);

			// Allocate buffers
			var grayscale = new byte[height * width];
			using var grayscaleBuffer = context.CreateBuffer(grayscale);

			var sobelDx = new byte[height * width];
			using var sobelDxBuffer = context.CreateBuffer(sobelDx);

			var sobelDy = new byte[height * width];
			using var sobelDyBuffer = context.CreateBuffer(sobelDy);

			var sobelGradient = new byte[height * width];
			using var sobelGradientBuffer = context.CreateBuffer(sobelGradient);

			var sobelDirection = new byte[height * width];
			using var sobelDirectionBuffer = context.CreateBuffer(sobelDirection);

			var nms = new byte[height * width];
			using var nmsBuffer = context.CreateBuffer(nms);

			// Enqueue kernels
			var events = new List<ComputeEventBase>();
			kernels.EnqueueGrayscale(device, globalWorkSize, image, grayscaleBuffer, events);
			kernels.EnqueueSobel(device, globalWorkSize, grayscaleBuffer, sobelDxBuffer, sobelDyBuffer, sobelGradientBuffer, sobelDirectionBuffer, events);
			kernels.EnqueueNonMaximumSuppression(device, globalWorkSize, sobelGradientBuffer, sobelDirectionBuffer, nmsBuffer, 10, events);

			await events.WaitForEvents();

			var sobelBitmap = sobelGradient.ToBitmap(width, height, 1);
			var sobelPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sobel_gradient.png"));
			sobelBitmap.Save(sobelPath);

			var nmsBitmap = nms.ToBitmap(width, height, 1);
			var nmsPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "nms.png"));
			nmsBitmap.Save(nmsPath);

			Run(sobelPath);
			Run(nmsPath);
		}
	}
}
