using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloo;
using Cloo.Bindings;
using Emphasis.ScreenCapture.OpenCL;
using Emphasis.ScreenCapture.Windows.Dxgi;
using Emphasis.TextDetection;
using NUnit.Framework;

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

			var device = ComputePlatform.Platforms
				.SelectMany(x => x.Devices)
				.First(x => x.Type == ComputeDeviceTypes.Gpu);

			using var context = new ComputeContext(new[] {device}, new ComputeContextPropertyList(device.Platform), null, IntPtr.Zero);			
			using var memory = await dispatcher.Dispatch(capture, context);

			using var program = new ComputeProgram(context, Kernels.grayscale);
			
			program.Build(new[] {device}, "-cl-std=CL1.2", (handle, ptr) => OnProgramBuilt(program, device), IntPtr.Zero);
			
			using var queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);
			using var kernel = program.CreateKernel("grayscale_u8");

			var size = capture.Height * capture.Width * 4;
			var result = new byte[size];
			var resultBuffer = new ComputeBuffer<byte>(
				context,
				ComputeMemoryFlags.WriteOnly | ComputeMemoryFlags.UseHostPointer,
				result);

			kernel.SetMemoryArgument(0, memory);
			kernel.SetMemoryArgument(1, resultBuffer);

			var globalWorkSize = new[] {(IntPtr) capture.Width, (IntPtr) capture.Height};
			var errorCode = CL10.EnqueueNDRangeKernel(
				queue.Handle, 
				kernel.Handle, 
				2, 
				null,
				globalWorkSize, 
				null, 
				0, 
				null, 
				null);

			if (errorCode != ComputeErrorCode.Success)
			{
				Console.WriteLine(errorCode);
			}

			queue.Finish();
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
