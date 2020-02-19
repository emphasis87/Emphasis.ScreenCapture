using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
			var screens =
				adapters.SelectMany(adapter =>
					adapter.Outputs.Select(output =>
						new Screen(adapter.Description.DeviceId, output.Description.DeviceName))
				).ToArray();
			return screens;
		}

		public async IAsyncEnumerable<Screen[]> GetScreenChanges(
			TimeSpan? pollingInterval = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var interval = pollingInterval ?? TimeSpan.FromSeconds(1);
			var time = DateTime.Now;
			var current = GetScreens();
			var currentSet = current.ToHashSet();
			yield return current;

			while (!cancellationToken.IsCancellationRequested)
			{
				var now = DateTime.Now;
				var diff = now - time;
				if (diff < pollingInterval)
					await Task.Delay(interval - diff, cancellationToken);

				time = DateTime.Now;
				var next = GetScreens();
				var nextSet = next.ToHashSet();
				if (currentSet.SetEquals(nextSet)) 
					continue;

				current = next;
				currentSet = nextSet;
				yield return current;
			}
		}

		public async IAsyncEnumerable<ScreenCapture> CaptureAll(
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var screens in GetScreenChanges(TimeSpan.FromSeconds(1), cancellationToken))
			{
				
			}

			yield break;
		}

		public void Dispose()
		{
			if (_factory1.IsValueCreated)
				_factory1.Value.Dispose();
		}
	}
}