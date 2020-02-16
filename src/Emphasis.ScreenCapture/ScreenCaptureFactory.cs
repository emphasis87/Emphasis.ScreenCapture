using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture
{
	public class ScreenCaptureFactory : IDisposable
	{
		private Lazy<Factory1> _factory1 =
			new Lazy<Factory1>(() => new Factory1());

		protected Factory1 Factory1 => _factory1.Value;

		public async IAsyncEnumerable<ScreenCapture> CaptureAll(
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
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