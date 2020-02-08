using System.Collections.Generic;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureMethodFactory
	{
		IEnumerable<ScreenCaptureMethod> Create();
	}
}