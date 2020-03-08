using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Cloo;
using Emphasis.OpenCL;
using Emphasis.OpenCL.Helpers;

namespace Emphasis.TextDetection
{
	public class Kernels : IDisposable, ICancelable
	{
		private readonly IComputeManager _computeManager;

		public Kernels(IComputeManager computeManager)
		{
			_disposable.Add(computeManager);

			const string options = "-cl-std=CL1.2";
			computeManager.AddProgram(KernelSources.threshold, options);
			computeManager.AddProgram(KernelSources.grayscale, options);
			computeManager.AddProgram(KernelSources.sobel, options);
			computeManager.AddProgram(KernelSources.canny, options);
			computeManager.AddProgram(KernelSources.non_maximum_supression, options);

			_computeManager = computeManager;
		}

		public void EnqueueGrayscale(
			ComputeDevice device,
			long[] globalWorkSize, 
			ComputeImage2D image, 
			ComputeBuffer<byte> grayscaleBuffer, 
			ICollection<ComputeEventBase> events)
		{
			events ??= new List<ComputeEventBase>();

			var kernel = _computeManager.GetKernel(device, "grayscale_u8");
			var queue = _computeManager.GetQueue(device);

			kernel.SetMemoryArgument(0, image);
			kernel.SetMemoryArgument(1, grayscaleBuffer);

			queue.Enqueue(kernel, globalWorkSize: globalWorkSize, events: events);
		}

		public void EnqueueThreshold(
			ComputeDevice device,
			ComputeBuffer<byte> sourceBuffer,
			ComputeBuffer<byte> thresholdBuffer,
			byte threshold,
			byte lowerThanValue,
			byte higherThanValue,
			ICollection<ComputeEventBase> events)
		{
			events ??= new List<ComputeEventBase>();

			var kernel = _computeManager.GetKernel(device, "threshold_u8");
			var queue = _computeManager.GetQueue(device);

			kernel.SetMemoryArgument(0, sourceBuffer);
			kernel.SetMemoryArgument(1, thresholdBuffer);
			kernel.SetValueArgument(2, threshold);
			kernel.SetValueArgument(3, lowerThanValue);
			kernel.SetValueArgument(4, higherThanValue);

			queue.Enqueue(kernel, globalWorkSize: new[] {sourceBuffer.Count}, events: events);
		}

		public void EnqueueSobel(
			ComputeDevice device, 
			long[] globalWorkSize,
			ComputeBuffer<byte> grayscaleBuffer,
			ComputeBuffer<byte> sobelDxBuffer,
			ComputeBuffer<byte> sobelDyBuffer,
			ComputeBuffer<byte> sobelGradientBuffer,
			ComputeBuffer<byte> sobelDirectionBuffer,
			ICollection<ComputeEventBase> events)
		{
			events ??= new List<ComputeEventBase>();

			var kernel = _computeManager.GetKernel(device, "sobel_u8");
			var queue = _computeManager.GetQueue(device);
			
			kernel.SetMemoryArgument(0, grayscaleBuffer);
			kernel.SetMemoryArgument(1, sobelDxBuffer);
			kernel.SetMemoryArgument(2, sobelDyBuffer);
			kernel.SetMemoryArgument(3, sobelGradientBuffer);
			kernel.SetMemoryArgument(4, sobelDirectionBuffer);

			queue.Enqueue(kernel, globalWorkSize: globalWorkSize, events: events);
		}

		public void EnqueueNonMaximumSuppression(
			ComputeDevice device,
			long[] globalWorkSize,
			ComputeBuffer<byte> sobelGradientBuffer,
			ComputeBuffer<byte> sobelDirectionBuffer,
			ComputeBuffer<byte> nmsBuffer,
			int minGradient,
			ICollection<ComputeEventBase> events)
		{
			events ??= new List<ComputeEventBase>();

			var kernel = _computeManager.GetKernel(device, "non_maximum_suppression_u8");
			var queue = _computeManager.GetQueue(device);

			kernel.SetMemoryArgument(0, sobelGradientBuffer);
			kernel.SetMemoryArgument(1, sobelDirectionBuffer);
			kernel.SetMemoryArgument(2, nmsBuffer);
			kernel.SetValueArgument(3, minGradient);

			queue.Enqueue(kernel, globalWorkSize: globalWorkSize, events: events);
		}

		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		public bool IsDisposed => _disposable.IsDisposed;

		public void Dispose()
		{
			_disposable.Dispose();
		}
	}
}