using System;
using System.Linq;
using Cloo;
using Cloo.Bindings;

namespace Emphasis.OpenCL.Helpers
{
	public static class QueueHelper
	{
		public static ComputeErrorCode Enqueue(
			this ComputeCommandQueue queue,
			ComputeKernel kernel,
			int[] dimensions)
		{
			var globalWorkSize = dimensions.Select(x => (IntPtr) x).ToArray();
			var errorCode = CL10.EnqueueNDRangeKernel(
				queue.Handle,
				kernel.Handle,
				globalWorkSize.Length,
				null,
				globalWorkSize,
				null,
				0,
				null,
				null);

			return errorCode;
		}
	}
}