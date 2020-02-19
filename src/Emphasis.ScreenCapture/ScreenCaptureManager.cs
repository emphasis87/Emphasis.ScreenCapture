using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture
{
	public class ScreenCaptureManager
	{
		public Screen[] GetScreens()
		{
			using var factory = new Factory1();
			var adapters = factory.Adapters1;
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
			IScreenCaptureMethodSelector selector = default,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			selector ??= new ScreenCaptureMethodSelector();

			await foreach (var screens in GetScreenChanges(TimeSpan.FromSeconds(1), cancellationToken))
			{
				
			}

			yield break;
		}
	}
}