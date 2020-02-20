using System.Collections.Generic;
using System.Threading;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureMethod
	{
		IAsyncEnumerable<ScreenCapture> Capture(Screen screen, CancellationToken cancellationToken = default);

		
	}
}