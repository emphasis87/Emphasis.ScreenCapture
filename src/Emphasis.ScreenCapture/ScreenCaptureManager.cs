using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Emphasis.ScreenCapture.Helpers;
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
			if (cancellationToken.IsCancellationRequested)
				yield break;

			selector ??= new ScreenCaptureMethodSelector();
			
			var screenChanges = GetScreenChanges(TimeSpan.FromSeconds(1), cancellationToken)
				.GetAsyncEnumerator(cancellationToken);

			if (!await screenChanges.MoveNextAsync())
				yield break;

			while (!cancellationToken.IsCancellationRequested)
			{
				var screens = screenChanges.Current;
				var cts = new CancellationTokenSource();
				var compositeToken =
					CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token;
				
				var _ = Task.Run(async () =>
				{
					await screenChanges.MoveNextAsync();
					cts.Cancel();
				}, cancellationToken);

				await foreach (var capture in screens
					.Select(screen => Capture(screen, selector, compositeToken))
					.Zip(compositeToken))
					yield return capture;
			}
		}

		public async IAsyncEnumerable<ScreenCapture> Capture(
			Screen screen,
			IScreenCaptureMethodSelector selector = default,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			selector ??= new ScreenCaptureMethodSelector();

			foreach (var method in selector.GetMethods())
			{
				var captureEnumerator = method.Capture(screen, cancellationToken)
					.GetAsyncEnumerator(cancellationToken);
				
				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						if (!await captureEnumerator.MoveNextAsync(cancellationToken))
							break;
					}
					catch (OperationCanceledException)
					{
						yield break;
					}
					catch
					{
						break;
					}

					yield return captureEnumerator.Current;
				}
			}

		}
	}
}