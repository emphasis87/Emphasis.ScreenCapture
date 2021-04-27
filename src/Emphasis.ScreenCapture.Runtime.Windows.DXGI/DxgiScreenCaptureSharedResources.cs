using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	internal class DxgiScreenCaptureSharedResources : ICancelable
	{
		private int _streams;
		private int _captures;
		private bool _hasChanged;
		private ValueTask _dispose;

		private readonly CompositeDisposable _disposable = new();

		public Factory1 Factory { get; }
		public Adapter1 Adapter { get; }
		public Output Output { get; }
		public Device Device { get; }
		public Output1 Output1 { get; }
		public OutputDuplication OutputDuplication { get; }
		
		public DxgiScreenCaptureSharedResources(
			[NotNull] Factory1 factory,
			[NotNull] Adapter1 adapter,
			[NotNull] Output output,
			[NotNull] Device device,
			[NotNull] Output1 output1,
			[NotNull] OutputDuplication outputDuplication)
		{
			Factory = factory;
			Adapter = adapter;
			Output = output;
			Device = device;
			Output1 = output1;
			OutputDuplication = outputDuplication;

			_disposable.Add(factory);
			_disposable.Add(adapter);
			_disposable.Add(output);
			_disposable.Add(device);
			_disposable.Add(output1);
			_disposable.Add(outputDuplication);
		}

		public bool IsFrameAcquired => Volatile.Read(ref _captures) > 0;

		public void AddReference()
		{
			Interlocked.Increment(ref _captures);
			_hasChanged = true;
		}

		public void RemoveReference()
		{
			var captures = Interlocked.Decrement(ref _captures);
			if (captures != 0) 
				return;

			var streams = Volatile.Read(ref _streams);
			if (streams == 0)
			{
				if (_dispose.IsCompleted)
					_dispose = DisposeDelayed();
			}
		}

		public void Acquire()
		{
			Interlocked.Increment(ref _streams);
			_hasChanged = true;
		}

		public void Release()
		{
			var streams = Interlocked.Decrement(ref _streams);
			if (streams != 0)
				return;

			var captures = Volatile.Read(ref _captures);
			if (captures == 0)
			{
				if (_dispose.IsCompleted)
					_dispose = DisposeDelayed();
			}
		}

		private async ValueTask DisposeDelayed()
		{
			_hasChanged = false;

			// Keep the shared resource in use for the next 1s in case it will be needed
			await Task.Delay(1000);

			if (_hasChanged)
				return;
			
			var streams = Volatile.Read(ref _streams);
			if (streams != 0)
				return;

			var captures = Volatile.Read(ref _captures);
			if (captures != 0)
				return;
		
			Dispose();
		}

		public bool IsDisposed => _disposable.IsDisposed;

		public void Dispose()
		{
			_disposable.Dispose();
		}
	}
}
