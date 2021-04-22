using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	internal class DxgiScreenCaptureSharedResources
	{
		private int _streams;
		private int _captures;
		private readonly CompositeDisposable _disposable = new CompositeDisposable();

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
		}

		public void RemoveReference()
		{
			var captures = Interlocked.Decrement(ref _captures);
			if (captures != 0) 
				return;

			var streams = Volatile.Read(ref _streams);
			if (streams == 0)
				_ = Dispose();
		}

		public void Acquire()
		{
			Interlocked.Increment(ref _streams);
		}

		public void Release()
		{
			Interlocked.Decrement(ref _streams);

			var captures = Volatile.Read(ref _captures);
			if (captures == 0)
				_ = Dispose();
		}

		private async ValueTask Dispose()
		{
			// Keep the shared resource in use for the next 1s in case it will be needed
			await Task.Delay(1000);

			var streams = Volatile.Read(ref _streams);
			if (streams == 0)
				_disposable.Dispose();
		}
	}
}
