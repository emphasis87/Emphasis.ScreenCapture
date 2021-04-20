using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCapture : IDisposable, ICancelable
	{
		public IScreen Screen { get; }
		public IScreenCaptureMethod Method { get; }

		void Add([NotNull] IDisposable disposable);
		void Remove([NotNull] IDisposable disposable);
	}

	public class ScreenCapture : IScreenCapture
	{
		public IScreen Screen { get; }
		public IScreenCaptureMethod Method { get; }
		public DateTime Timestamp { get; }
		public int Width { get; }
		public int Height { get; }

		public ScreenCapture(
			[NotNull] IScreen screen,
			[NotNull] IScreenCaptureMethod method,
			DateTime timestamp,
			int width,
			int height)
		{
			Screen = screen;
			Timestamp = timestamp;
			Width = width;
			Height = height;
			Method = method;
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
			if (IsDisposed) 
				throw new ObjectDisposedException($"{GetType()} instance is already disposed.");
			
			_disposable.Add(disposable);
		}

		public void Remove([NotNull] IDisposable disposable)
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"{GetType()} instance is already disposed.");

			_disposable.Remove(disposable);
		}
		#endregion
	}
}
