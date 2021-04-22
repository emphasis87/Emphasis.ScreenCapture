using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCapture : IDisposable, ICancelable
	{
		public IScreen Screen { get; }
		public IServiceProvider ServiceProvider { get; }

		void Add([NotNull] IDisposable disposable);
		void Remove([NotNull] IDisposable disposable);
	}

	public class ScreenCapture : IScreenCapture
	{
		public IScreen Screen { get; }
		public IServiceProvider ServiceProvider { get; }
		public DateTime Timestamp { get; }
		public int Width { get; }
		public int Height { get; }

		public ScreenCapture(
			[NotNull] IScreen screen,
			[NotNull] IServiceProvider serviceProvider,
			DateTime timestamp,
			int width,
			int height)
		{
			Screen = screen ?? throw new ArgumentNullException(nameof(screen));
			ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			Timestamp = timestamp;
			Width = width;
			Height = height;
		}

		#region IDisposable, ICancelable
		public bool IsDisposed => _disposable.IsDisposed;
		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		public virtual void Dispose()
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
