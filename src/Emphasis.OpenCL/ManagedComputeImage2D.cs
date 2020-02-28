using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Cloo;

namespace Emphasis.OpenCL
{
	public class ManagedComputeImage2D : ComputeImage2D, ICancelable
	{
		[Obsolete("Deprecated in OpenCL 1.2.")]
		public ManagedComputeImage2D(
			ComputeContext context, 
			ComputeMemoryFlags flags, 
			ComputeImageFormat format, 
			int width, 
			int height, 
			long rowPitch, 
			IntPtr data) 
			: base(context, flags, format, width, height, rowPitch, data)
		{
		}

		public bool IsDisposed => _disposable.IsDisposed;
		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		protected override void Dispose(bool manual)
		{
			base.Dispose(manual);

			_disposable.Dispose();
		}

		public void Add([NotNull] IDisposable disposable)
		{
			_disposable.Add(disposable);
		}

		public void Remove([NotNull] IDisposable disposable)
		{
			_disposable.Remove(disposable);
		}
	}
}