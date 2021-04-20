using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiTexture : IDisposable, ICancelable
	{
		public IntPtr DataPointer;
		public int RowPitch;
		public int SlicePitch;

		public DxgiTexture(IntPtr dataPointer, int rowPitch, int slicePitch)
		{
			DataPointer = dataPointer;
			RowPitch = rowPitch;
			SlicePitch = slicePitch;
		}

		#region IDisposable, ICancelable
		public bool IsDisposed => _disposable.IsDisposed;
		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		public void Dispose()
		{
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
		#endregion
	}
}