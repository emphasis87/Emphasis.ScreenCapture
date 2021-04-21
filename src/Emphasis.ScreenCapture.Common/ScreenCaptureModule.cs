using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureModule
	{
		void Configure(IServiceCollection services);
	}
}
