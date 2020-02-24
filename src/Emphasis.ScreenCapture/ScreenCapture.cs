using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;

namespace Emphasis.ScreenCapture
{
	public class ScreenCapture : IDisposable, ICancelable
	{
		public Screen Screen { get; }
		public DateTime Time { get; }
		public int Width { get; }
		public int Height { get; }

		public IScreenCaptureMethod Method { get; }

		public ScreenCapture(
			[NotNull] Screen screen,
			DateTime time,
			int width,
			int height,
			[NotNull] IScreenCaptureMethod method)
		{
			Screen = screen;
			Time = time;
			Width = width;
			Height = height;
			Method = method;
		}

		public bool IsDisposed => _disposable.IsDisposed;
		private readonly CompositeDisposable _disposable =new CompositeDisposable();

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
	}
}
