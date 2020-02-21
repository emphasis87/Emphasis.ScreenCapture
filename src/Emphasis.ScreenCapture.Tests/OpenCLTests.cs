using System;
using System.Linq;
using Cloo;
using Emphasis.ScreenCapture.OpenCL;
using NUnit.Framework;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture.Tests
{
	public class OpenCLTests
	{
		[Test]
		public void Opencl()
		{
			foreach (var platform in ComputePlatform.Platforms)
			{
				Console.WriteLine($"Platform Name: {platform.Name}");
				Console.WriteLine($"Platform Vendor: {platform.Vendor}");
				Console.WriteLine($"Platform Extensions: {platform.Extensions.Aggregate((x, y) => $"{x} {y}")}");

				if (CL12x.TryFindClGetDeviceIDsFromD3D11KHR(platform, out var clGetDeviceIDsFromD3D11KHR))
				{
					Console.WriteLine("D3D11 KHR sharing");

					var factory = new Factory1();
					foreach (var adapter1 in factory.Adapters1)
					{
						var devices = new IntPtr[10];
						clGetDeviceIDsFromD3D11KHR(
							platform.Handle.Value,
							cl_d3d11_device_source_khr.CL_D3D11_DEVICE_KHR,
							adapter1.NativePointer,
							cl_d3d11_device_set_khr.CL_ALL_DEVICES_FOR_D3D11_KHR,
							10,
							devices,
							out var numDevices);

						if (numDevices > 0)
							Console.WriteLine($"\tAdapter {adapter1.Description1.Description}: {numDevices}");

						foreach (var deviceId in devices)
						{
							var oclDevice = platform.Devices.FirstOrDefault(x => x.Handle.Value == deviceId);
							if (oclDevice != null)
								Console.WriteLine($"\t\t{oclDevice.Name}");
						}

					}
				}
				if (CL12x.TryFindClGetDeviceIDsFromD3D11NV(platform, out var clGetDeviceIDsFromD3D11NV))
				{
					Console.WriteLine("D3D11 NV sharing");

					var factory = new Factory1();
					foreach (var adapter1 in factory.Adapters1)
					{
						var devices = new IntPtr[10];
						clGetDeviceIDsFromD3D11NV(
							platform.Handle.Value,
							cl_d3d11_device_source_nv.CL_D3D11_DXGI_ADAPTER_NV,
							adapter1.NativePointer,
							cl_d3d11_device_set_nv.CL_ALL_DEVICES_FOR_D3D11_NV,
							10,
							devices,
							out var numDevices);

						if (numDevices > 0)
							Console.WriteLine($"\tAdapter {adapter1.Description1.Description}: {numDevices}");

						foreach (var deviceId in devices)
						{
							var oclDevice = platform.Devices.FirstOrDefault(x => x.Handle.Value == deviceId);
							if (oclDevice != null)
								Console.WriteLine($"\t\t{oclDevice.Name}");
						}
						
					}
				}

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