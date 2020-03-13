using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloo;

namespace Emphasis.OpenCL.Helpers
{
	public static class QueueHelper
	{
		public static async Task ExecuteAsync(
			this ComputeCommandQueue queue,
			ComputeKernel kernel,
			long[] globalWorkOffset = null,
			long[] globalWorkSize = null,
			long[] localWorkSize = null,
			ICollection<ComputeEventBase> events = null)
		{
			events ??= new List<ComputeEventBase>();

			queue.Execute(kernel, globalWorkOffset, globalWorkSize, localWorkSize, events);

			await events.WaitForEvents();
		}

		public static void Enqueue(
			this ComputeCommandQueue queue,
			ComputeKernel kernel,
			long[] globalWorkOffset = null,
			long[] globalWorkSize = null,
			long[] localWorkSize = null,
			ICollection<ComputeEventBase> events = null)
		{
			events ??= new List<ComputeEventBase>();

			queue.Execute(kernel, globalWorkOffset, globalWorkSize, localWorkSize, events);
		}

		public static async Task WaitForEvents(this ICollection<ComputeEventBase> events)
		{
			if (events.Count == 0)
				return;

			var notCompleted = events.Where(x => x.Status != ComputeCommandExecutionStatus.Complete).ToArray();
			if (notCompleted.Length == 0)
				return;

			var count = 0;
			var tcs = new TaskCompletionSource<bool>();
			foreach (var evt in notCompleted)
			{
				if (evt.Status == ComputeCommandExecutionStatus.Complete)
					continue;

				count++;
				evt.Completed += delegate (object sender, ComputeCommandStatusArgs args)
				{
					if (notCompleted.All(x => x.Status == ComputeCommandExecutionStatus.Complete))
						tcs.TrySetResult(true);
				};
				evt.Aborted += delegate (object sender, ComputeCommandStatusArgs args)
				{
					tcs.TrySetCanceled();
				};
			}

			if (count > 0)
				await tcs.Task;
		}
	}
}