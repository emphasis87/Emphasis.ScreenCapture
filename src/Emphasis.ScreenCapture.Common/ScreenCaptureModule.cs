using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureModule : IServiceProvider
	{
		int Priority { get; }

		void Configure(IServiceCollection services);
		void Configure(IServiceProvider serviceProvider);
	}

	public abstract class ScreenCaptureModule : IScreenCaptureModule
	{
		public abstract int Priority { get; }
		public IServiceProvider ServiceProvider { get; private set; }

		private static readonly ConcurrentDictionary<Type, Lazy<IServiceCollection>> ServicesByModule = new();

		public void Configure(IServiceCollection services)
		{
			var current = ServicesByModule.GetOrAdd(GetType(), new Lazy<IServiceCollection>(CreateServices));
			services.Add(current.Value);
		}

		protected abstract IServiceCollection CreateServices();

		public void Configure(IServiceProvider serviceProvider)
		{
			ServiceProvider = serviceProvider;
		}

		public object GetService(Type serviceType)
		{
			return ServiceProvider.GetService(serviceType);
		}
	}
}
