using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cloo;
using Emphasis.OpenCL.Helpers;
using Emphasis.ScreenCapture.Helpers;
using Emphasis.ScreenCapture.OpenCL;
using Emphasis.TextDetection;
using NUnit.Framework;
using static Emphasis.ScreenCapture.Helpers.DebugHelper;

namespace Emphasis.ScreenCapture.Tests
{
	public class ComputeMemoryDispatcherTests
	{
		[Test]
		public async Task Can_Dispatch()
		{
			var manager = new ScreenCaptureManager();
			var dispatcher = new ComputeMemoryDispatcher();

			var screen = manager.GetScreens().First();

			using var capture = await manager.Capture(screen).FirstAsync();
			var width = capture.Width;
			var height = capture.Height;

			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var context = new ComputeContext(new[] {device}, new ComputeContextPropertyList(device.Platform), null, IntPtr.Zero);			
			using var memory = await dispatcher.Dispatch(capture, context);

			using var program = new ComputeProgram(context, Kernels.grayscale);

			program.Build(new[] {device}, "-cl-std=CL1.2", null, IntPtr.Zero);
			
			using var queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);
			using var kernel = program.CreateKernel("grayscale_u8");

			var target = new byte[width * height];
			using var resultBuffer = context.CreateBuffer(target);

			kernel.SetMemoryArgument(0, memory);
			kernel.SetMemoryArgument(1, resultBuffer);

			var errorCode = queue.Enqueue(kernel, new[] {width, height});
			if (errorCode != ComputeErrorCode.Success)
			{
				Console.WriteLine(errorCode);
			}

			queue.Finish();

			var result = target.ToBitmap(width, height, 1);
			var resultPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "grayscale.png"));
			result.Save(resultPath);

			Run(resultPath);
		}

		private void OnProgramBuilt(ComputeProgram program, ComputeDevice device)
		{
			var status = program.GetBuildStatus(device);
			if (status == ComputeProgramBuildStatus.Error)
			{
				var log = program.GetBuildLog(device);
				Console.WriteLine(log);
			}
		}
	}
}
