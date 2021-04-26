using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureOclImageFactory
	{
		Task<IOclImage> CreateImage(IScreenCapture capture, nint contextId, nint queueId);
	}

	public static class ScreenCaptureOclExtensions
	{
		private static IScreenCaptureOclImageFactory GetFactory(IScreenCapture capture)
		{
			var serviceProvider = capture.ServiceProvider
				?? throw new ArgumentNullException(nameof(capture.ServiceProvider), $"{nameof(capture.ServiceProvider)} is null.");
			var factory = serviceProvider.GetService<IScreenCaptureOclImageFactory>()
				?? throw new NotSupportedException($"{capture.GetType()} does not support {typeof(IScreenCaptureOclImageFactory)}.");
			return factory;
		}

		public static Task<IOclImage> CreateImage(this IScreenCapture capture, nint contextId, nint queueId)
		{
			var factory = GetFactory(capture);
			return factory.CreateImage(capture, contextId, queueId);
		}
	}
}
