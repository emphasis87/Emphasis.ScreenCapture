using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureOclImageFactory
	{
		Task<nint> CreateImage(IScreenCapture capture, nint contextId);
		Task<nint> CreateOrCopyToImage(IScreenCapture capture, nint clContext, nint clImage);
		Task<nint> CopyToImage(IScreenCapture capture, nint clContext, nint clImage);

		nint CreateImage(IScreenCapture capture, nint contextId, nint clEvent);
		nint CreateOrCopyToImage(IScreenCapture capture, nint clContext, nint clImage, nint clEvent);
		nint CopyToImage(IScreenCapture capture, nint clContext, nint clImage, nint clEvent);
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

		public static Task<nint> CreateImage(this IScreenCapture capture, nint clContext)
		{
			var factory = GetFactory(capture);
			return factory.CreateImage(capture, clContext);
		}

		public static Task<nint> CreateOrCopyToImage(this IScreenCapture capture, nint clContext, nint clImage)
		{
			var factory = GetFactory(capture);
			return factory.CreateOrCopyToImage(capture, clContext, clImage);
		}

		public static Task<nint> CopyToImage(this IScreenCapture capture, nint clContext, nint clImage)
		{
			var factory = GetFactory(capture);
			return factory.CopyToImage(capture, clContext, clImage);
		}

		public static nint CreateImage(this IScreenCapture capture, nint clContext, nint clEvent)
		{
			var factory = GetFactory(capture);
			return factory.CreateImage(capture, clContext, clEvent);
		}

		public static nint CreateOrCopyToImage(this IScreenCapture capture, nint clContext, nint clImage, nint clEvent)
		{
			var factory = GetFactory(capture);
			return factory.CreateOrCopyToImage(capture, clContext, clImage, clEvent);
		}

		public static nint CopyToImage(this IScreenCapture capture, nint clContext, nint clImage, nint clEvent)
		{
			var factory = GetFactory(capture);
			return factory.CopyToImage(capture, clContext, clImage, clEvent);
		}
	}
}
