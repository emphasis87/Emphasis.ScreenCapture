using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using Cloo;

namespace Emphasis.OpenCL
{
	public interface IComputeManager : IDisposable, ICancelable
	{
		ComputeContext GetContext(ComputeDevice device);
		ComputeCommandQueue GetQueue(ComputeDevice device);
		ComputeKernel GetKernel(ComputeDevice device, string functionName);

		void AddProgram(string source, string options);
	}

	public class ComputeManager : IComputeManager
	{
		private readonly Dictionary<ComputeDevice, ComputeContext> _contexts = 
			new Dictionary<ComputeDevice, ComputeContext>();

		private readonly Dictionary<ComputeDevice, ComputeCommandQueue> _queues =
			new Dictionary<ComputeDevice, ComputeCommandQueue>();

		private readonly Dictionary<(ComputeDevice, string), ComputeKernel> _kernels =
			new Dictionary<(ComputeDevice, string), ComputeKernel>();

		public ComputeContext GetContext(ComputeDevice device)
		{
			if (_contexts.TryGetValue(device, out var context))
				return context;

			context = new ComputeContext(new[] { device }, new ComputeContextPropertyList(device.Platform), null, IntPtr.Zero);
			Add(context);
			_contexts.Add(device, context);
			return context;
		}

		public ComputeCommandQueue GetQueue(ComputeDevice device)
		{
			if (_queues.TryGetValue(device, out var queue))
				return queue;

			var context = GetContext(device);
			queue = new ComputeCommandQueue(context, device, device.CommandQueueFlags);
			Add(queue);
			_queues.Add(device, queue);
			return queue;
		}

		public ComputeKernel GetKernel(ComputeDevice device, string functionName)
		{
			if (!_kernels.TryGetValue((device, functionName), out var kernel))
				throw new ArgumentException(
					$"The kernel '{functionName}' has not been found. Use {nameof(AddProgram)} to add programs and their kernels to the {nameof(ComputeManager)}");

			return kernel;
		}

		public void AddProgram(string source, string options)
		{
			foreach (var platform in ComputePlatform.Platforms)
			{
				foreach (var device in platform.Devices)
				{
					var context = GetContext(device);
					var program = new ComputeProgram(context, source);

					var success = false;
					try
					{
						program.Build(new[] { device }, options, null, IntPtr.Zero);
						success = true;
					}
					catch (BuildProgramFailureComputeException ex)
					{
						Trace.WriteLine(ex.ToString());
						Trace.WriteLine(program.GetBuildLog(device));
						program.Dispose();
					}

					if (!success)
						continue;

					var kernels = program.CreateAllKernels();
					foreach (var kernel in kernels)
					{
						Add(kernel);
						_kernels.Add((device, kernel.FunctionName), kernel);
					}
				}
			}
		}

		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		public bool IsDisposed => _disposable.IsDisposed;

		public void Dispose()
		{
			_disposable.Dispose();
		}

		public void Add(IDisposable disposable)
		{
			_disposable.Add(disposable);
		}

		public void Remove(IDisposable disposable)
		{
			_disposable.Remove(disposable);
		}
	}
}