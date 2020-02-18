using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Emphasis.ScreenCapture.Windows.Dxgi;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture
{
	public class ScreenCaptureManager : IDisposable
	{
		private readonly Lazy<Factory1> _factory1 =
			new Lazy<Factory1>(() => new Factory1());

		protected Factory1 Factory1 => _factory1.Value;

		public Screen[] GetScreens()
		{
			var adapters = Factory1.Adapters1;
			var screens = adapters.SelectMany(adapter =>
					adapter.Outputs.Select(output =>
						new Screen(adapter, output.QueryInterface<Output1>())))
				.ToArray();
			return screens;
		}

		public async IAsyncEnumerable<Screen[]> GetScreenChanges(
			TimeSpan pollingInterval,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var current = GetScreens();
			yield return current;

			while (!cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(pollingInterval, cancellationToken);

			}
			
		}

		public async IAsyncEnumerable<ScreenCapture> CaptureAll(
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{

			var f = new DxgiScreenCaptureMethod();
			
			try
			{
				await foreach (var r in f.Capture(new ScreenCaptureSettings(),cancellationToken))
				{

				}
			}
			catch
			{

			}

			var outputs = Factory1.Adapters1.SelectMany(x => x.Outputs,
				(adapter, output) => new ScreenCaptureSettings()
				{
					Adapter = adapter
				});
			yield break;

		}

		public void Dispose()
		{
			if (_factory1.IsValueCreated)
				_factory1.Value.Dispose();
		}
	}
}