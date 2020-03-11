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
			kernels.EnqueueThreshold(device, grayscaleBuffer, thresholdBuffer, 123, events);

			await events.WaitForEvents();

			var result = threshold.ToBitmap(width, height, 1);
			var resultPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "threshold.png"));
			result.Save(resultPath);

			Run(resultPath);
		}

		[Test]
		public async Task Can_EnqueueGaussBlur()
		{
			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var computeManager = new ComputeManager();
			using var kernels = new Kernels(computeManager);

			var context = computeManager.GetContext(device);
			var sample = Samples.sample02;
			var sampleBytes = sample.ToBytes();
			
			var width = sample.Width;
			var height = sample.Height;
			var globalWorkSize = new long[] { width, height };

			using var image = context.CreateImage2D(sampleBytes, width, height);

			var grayscale = new byte[height * width];
			using var grayscaleBuffer = context.CreateBuffer(grayscale);

			var gaussBlur = new byte[height * width];
			using var gaussBlurBuffer = context.CreateBuffer(gaussBlur);

			var events = new List<ComputeEventBase>();
			kernels.EnqueueGrayscale(device, globalWorkSize, image, grayscaleBuffer, events);
			kernels.EnqueueGaussBlur(device, globalWorkSize, grayscaleBuffer, gaussBlurBuffer, events);

			await events.WaitForEvents();

			var grayscaleBitmap = grayscale.ToBitmap(width, height, 1);
			var grayscalePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "grayscale.png"));
			grayscaleBitmap.Save(grayscalePath);

			var gaussBlurBitmap = gaussBlur.ToBitmap(width, height, 1);
			var gaussBlurPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "gauss_blur.png"));
			gaussBlurBitmap.Save(gaussBlurPath);

			var gauss = new float[][]
			{
				new[] {0.0625f, 0.1250f, 0.0625f},
				new[] {0.1250f, 0.2500f, 0.1250f},
				new[] {0.0625f, 0.1250f, 0.0625f},
			};

			var gauss2 = new byte[height * width];
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					if (y == 0 || y == height - 1 || x == 0 || x == width - 1)
					{
						gauss2[d] = grayscale[d];
					}
					else
					{
						var sum =
							gauss[0][0] * grayscale[(y - 1) * width + (x - 1)] +
							gauss[0][1] * grayscale[(y - 1) * width + (x + 0)] +
							gauss[0][2] * grayscale[(y - 1) * width + (x + 1)] +
							gauss[1][0] * grayscale[(y + 0) * width + (x - 1)] +
							gauss[1][1] * grayscale[(y + 0) * width + (x + 0)] +
							gauss[1][2] * grayscale[(y + 0) * width + (x + 1)] +
							gauss[2][0] * grayscale[(y + 1) * width + (x - 1)] +
							gauss[2][1] * grayscale[(y + 1) * width + (x + 0)] +
							gauss[2][2] * grayscale[(y + 1) * width + (x + 1)];
						gauss2[d] = (byte)Math.Round(Math.Min(255, sum));
					}
				}
			}

			var gauss2Bitmap = gauss2.ToBitmap(width, height, 1);
			var gauss2Path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "gauss2.png"));
			gauss2Bitmap.Save(gauss2Path);

			Run(grayscalePath);
			Run(gaussBlurPath);
			Run(gauss2Path);
		}

		[Test]
		public async Task Canny_edge_detection_grayscale()
		{
			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var computeManager = new ComputeManager();
			using var kernels = new Kernels(computeManager);

			var context = computeManager.GetContext(device);
			var sample = Samples.sample02;
			var sampleBytes = sample.ToBytes();

			var width = sample.Width;
			var height = sample.Height;
			var globalWorkSize = new long[] { width, height };

			using var image = context.CreateImage2D(sampleBytes, width, height);

			// Allocate buffers
			var grayscale = new byte[height * width];
			using var grayscaleBuffer = context.CreateBuffer(grayscale);

			var sobelGradient = new byte[height * width];
			using var sobelGradientBuffer = context.CreateBuffer(sobelGradient);

			var sobelDirection = new byte[height * width];
			using var sobelDirectionBuffer = context.CreateBuffer(sobelDirection);

			var gaussBlur = new byte[height * width];
			using var gaussBlurBuffer = context.CreateBuffer(gaussBlur);

			var nms = new byte[height * width];
			using var nmsBuffer = context.CreateBuffer(nms);

			var threshold = new byte[height * width];
			using var thresholdBuffer = context.CreateBuffer(threshold);

			// Enqueue kernels
			var events = new List<ComputeEventBase>();
			kernels.EnqueueGrayscale(device, globalWorkSize, image, grayscaleBuffer, events);
			kernels.EnqueueGaussBlur(device, globalWorkSize, grayscaleBuffer, gaussBlurBuffer, events);
			kernels.EnqueueSobel(device, globalWorkSize, grayscaleBuffer, sobelGradientBuffer, sobelDirectionBuffer, events);
			kernels.EnqueueNonMaximumSuppression(device, globalWorkSize, sobelGradientBuffer, sobelDirectionBuffer, nmsBuffer, events);
			kernels.EnqueueDoubleThreshold(device, nmsBuffer, thresholdBuffer, 30, 70, events);

			await events.WaitForEvents();

			var grayscaleBitmap = grayscale.ToBitmap(width, height, 1);
			var grayscalePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "grayscale.png"));
			grayscaleBitmap.Save(grayscalePath);

			var sobelBitmap = sobelGradient.ToBitmap(width, height, 1);
			var sobelPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sobel_gradient.png"));
			sobelBitmap.Save(sobelPath);

			var nmsBitmap = nms.ToBitmap(width, height, 1);
			var nmsPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "nms.png"));
			nmsBitmap.Save(nmsPath);

			var thresholdBitmap = threshold.ToBitmap(width, height, 1);
			var thresholdPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "threshold.png"));
			thresholdBitmap.Save(thresholdPath);

			Run(grayscalePath);
			Run(sobelPath);
			Run(nmsPath);
			Run(thresholdPath);
		}

		[Test]
		public async Task Canny_edge_detection()
		{
			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var computeManager = new ComputeManager();
			using var kernels = new Kernels(computeManager);

			var context = computeManager.GetContext(device);
			var sample = Samples.sample02;
			var sampleBytes = sample.ToBytes();

			var width = sample.Width;
			var height = sample.Height;
			var globalWorkSize = new long[] { width, height };

			using var image = context.CreateImage2D(sampleBytes, width, height);

			// Allocate buffers
			var grayscale = new byte[height * width];
			using var grayscaleBuffer = context.CreateBuffer(grayscale);

			var sobelGradient = new byte[height * width];
			using var sobelGradientBuffer = context.CreateBuffer(sobelGradient);

			var sobelDirection = new byte[height * width];
			using var sobelDirectionBuffer = context.CreateBuffer(sobelDirection);

			var gaussBlur = new byte[height * width];
			using var gaussBlurBuffer = context.CreateBuffer(gaussBlur);

			var nms = new byte[height * width];
			using var nmsBuffer = context.CreateBuffer(nms);

			var threshold = new byte[height * width];
			using var thresholdBuffer = context.CreateBuffer(threshold);

			// Enqueue kernels
			var events = new List<ComputeEventBase>();
			kernels.EnqueueGrayscale(device, globalWorkSize, image, grayscaleBuffer, events);
			kernels.EnqueueGaussBlur(device, globalWorkSize, grayscaleBuffer, gaussBlurBuffer, events);
			kernels.EnqueueSobel(device, globalWorkSize, grayscaleBuffer, sobelGradientBuffer, sobelDirectionBuffer, events);
			kernels.EnqueueNonMaximumSuppression(device, globalWorkSize, sobelGradientBuffer, sobelDirectionBuffer, nmsBuffer, events);
			kernels.EnqueueDoubleThreshold(device, nmsBuffer, thresholdBuffer, 30, 70, events);

			await events.WaitForEvents();

			var grayscaleBitmap = grayscale.ToBitmap(width, height, 1);
			var grayscalePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "grayscale.png"));
			grayscaleBitmap.Save(grayscalePath);

			var sobelBitmap = sobelGradient.ToBitmap(width, height, 1);
			var sobelPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sobel_gradient.png"));
			sobelBitmap.Save(sobelPath);

			var nmsBitmap = nms.ToBitmap(width, height, 1);
			var nmsPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "nms.png"));
			nmsBitmap.Save(nmsPath);

			var thresholdBitmap = threshold.ToBitmap(width, height, 1);
			var thresholdPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "threshold.png"));
			thresholdBitmap.Save(thresholdPath);

			Run(grayscalePath);
			Run(sobelPath);
			Run(nmsPath);
			Run(thresholdPath);
		}
	}
}
