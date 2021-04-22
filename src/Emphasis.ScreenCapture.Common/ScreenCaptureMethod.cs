using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureMethod
	{
		Task<IScreenCapture> Capture(IScreen screen, CancellationToken cancellationToken = default);
		IAsyncEnumerable<IScreenCapture> CaptureStream(IScreen screen, CancellationToken cancellationToken = default);
	}
}