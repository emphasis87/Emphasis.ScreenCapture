using System;
using System.Diagnostics;
using System.Linq;
using Cloo;
using NUnit.Framework;
using SharpDX;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Tests
{
	public class DxgiTests
	{
		private const int Count = 10000;

		/// <summary>
		/// Retrieving the list of outputs is quite cheap (&lt;0.1ms).
		/// </summary>
		[Test]
		public void Get_all_outputs_benchmark()
		{
			var factory = new Factory1();

			var sw = Stopwatch.StartNew();
			for (var i = 0; i < Count; i++)
			{
				var adapters = factory.Adapters;
				foreach (var adapter in adapters)
				{
					var outputs = adapter.Outputs;
					foreach (var output in outputs)
					{
						
					}
				}
			}
			sw.Stop();

			var microseconds = (sw.ElapsedTicks/Count) / (TimeSpan.TicksPerMillisecond / 1000);
			Console.WriteLine($"Collecting outputs: {microseconds}microseconds");
		}

		[Test]
		public void Get_all_outputs()
		{
			var factory = new Factory1();
			var adapters = factory.Adapters;
			foreach (var adapter in adapters)
			{
				Console.WriteLine($"Description: |{adapter.Description.Description}|");
				Console.WriteLine($"VendorId: {adapter.Description.VendorId}");
				Console.WriteLine($"DeviceId: {adapter.Description.DeviceId}");

				var outputs = adapter.Outputs;
				foreach (var output in outputs)
				{
					Console.WriteLine($"\tDeviceName: |{output.Description.DeviceName}|");
					Console.WriteLine($"\tIsAttachedToDesktop: {output.Description.IsAttachedToDesktop}");
					var bounds = (Rectangle) output.Description.DesktopBounds;
					Console.WriteLine($"\tDesktopBounds: Height: {bounds.Height}, Width: {bounds.Width}");
					Console.WriteLine($"\t===");
				}
			}
		}

		[Test]
		public void Opencl()
		{
			foreach (var platform in ComputePlatform.Platforms)
			{
				Console.WriteLine($"Platform Name: {platform.Name}");
				Console.WriteLine($"Platform Vendor: {platform.Vendor}");
				Console.WriteLine($"Platform Extensions: {platform.Extensions.Aggregate((x,y) => $"{x} {y}")}");

				foreach (var device in platform.Devices)
				{
					Console.WriteLine($"\tDevice Name: {device.Name}");
					Console.WriteLine($"\tDevice Type: {device.Type}");
					Console.WriteLine($"\tDevice Vendor: {device.Vendor}");
					Console.WriteLine($"\tDevice VendorId: {device.VendorId}");
					Console.WriteLine($"\tDevice Max Compute Units: {device.MaxComputeUnits}");
					Console.WriteLine($"\tDevice Global Memory: {device.GlobalMemorySize}");
					Console.WriteLine($"\tDevice Max Clock Frequency: {device.MaxClockFrequency}");
					Console.WriteLine($"\tDevice Max Allocatable Memory: {device.MaxMemoryAllocationSize}");
					Console.WriteLine($"\tDevice Local Memory: {device.LocalMemorySize}");
					Console.WriteLine($"\tDevice Max Work-group size: {device.MaxWorkGroupSize}");
					Console.WriteLine($"\tDevice Available: {device.Available}");
					Console.WriteLine($"\tDevice Extensions: {device.Extensions.Aggregate((x, y) => $"{x} {y}")}");
					Console.WriteLine($"\t===");
				}
			}
		}
	}
}
