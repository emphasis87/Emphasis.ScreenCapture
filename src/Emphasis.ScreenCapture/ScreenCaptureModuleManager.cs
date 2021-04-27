using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Emphasis.ScreenCapture
{
	public interface IScreenCaptureModuleManager
	{
		IEnumerable<IScreenCaptureModule> Modules { get; }
		IServiceProvider ServiceProvider { get; }
	}

	public class ScreenCaptureModuleManager : IScreenCaptureModuleManager
	{
		private readonly object _gate = new();
		private IServiceProvider _serviceProvider;

		private List<IScreenCaptureModule> _modules;
		public IEnumerable<IScreenCaptureModule> Modules => _modules?.AsReadOnly() ?? Enumerable.Empty<IScreenCaptureModule>();

		public ScreenCaptureModuleManager(IEnumerable<IScreenCaptureModule> modules = null)
		{
			_modules = modules?.ToList();
		}

		public IServiceProvider ServiceProvider
		{
			get
			{
				var serviceProvider = Volatile.Read(ref _serviceProvider);
				if (serviceProvider != null)
					return serviceProvider;

				lock (_gate)
				{
					serviceProvider = Volatile.Read(ref _serviceProvider);
					if (serviceProvider != null)
						return serviceProvider;

					var services = new ServiceCollection();

					var modules = _modules ??= LoadModules();
					var orderedModules = modules.OrderBy(x => x.Priority).ToList();
					foreach (var module in orderedModules)
					{
						module.Configure(services);
					}

					serviceProvider = services.BuildServiceProvider();

					Volatile.Write(ref _serviceProvider, serviceProvider);

					foreach (var module in orderedModules)
					{
						module.Configure(serviceProvider);
					}

					return serviceProvider;
				}
			}
		}

		private static List<IScreenCaptureModule> LoadModules()
		{
			string platform;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				platform = "windows";
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				platform = "linux";
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				platform = "osx";
			else
				platform = RuntimeInformation.OSDescription;

			var name = $"ScreenCapture.Runtime.{platform}";
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(x => x.GetName().Name.Contains(name, StringComparison.OrdinalIgnoreCase))
				.ToList();

			var directory = AppDomain.CurrentDomain.BaseDirectory;
			var files = Directory.GetFiles(directory, "*.dll")
				.Where(x => x.Contains(name, StringComparison.OrdinalIgnoreCase))
				.ToHashSet();

			foreach (var assembly in assemblies)
			{
				var file = Path.GetFileName(assembly.Location);
				files.Remove(file);
			}

			foreach (var file in files)
			{
				try
				{
					var reflectionOnlyAssembly = Assembly.ReflectionOnlyLoadFrom(Path.Combine(directory, file));
					var isModule = reflectionOnlyAssembly.GetExportedTypes()
						.Any(x => typeof(IScreenCaptureModule).IsAssignableFrom(x));
					if (!isModule)
						continue;
				}
				catch (NotSupportedException)
				{
				}

				var assembly = Assembly.LoadFrom(Path.Combine(directory, file));
				assemblies.Add(assembly);
			}

			var moduleTypes = assemblies.SelectMany(x => x.GetExportedTypes())
				.Where(x => typeof(IScreenCaptureModule).IsAssignableFrom(x))
				.ToList();

			var modules = new List<IScreenCaptureModule>();
			foreach (var moduleType in moduleTypes)
			{
				var module = (IScreenCaptureModule) Activator.CreateInstance(moduleType);
				modules.Add(module);
			}

			return modules;
		}
	}
}