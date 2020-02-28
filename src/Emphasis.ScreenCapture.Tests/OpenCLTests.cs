using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Cloo;
using Emphasis.OpenCL.Extensions;
using Emphasis.OpenCL.Helpers;
using Emphasis.ScreenCapture.Helpers;
using NUnit.Framework;
using SharpDX.DXGI;

using static Emphasis.ScreenCapture.Helpers.DebugHelper;
using CL12x = Emphasis.OpenCL.Extensions.CL12x;

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
					var count = 0;
					var factory = new Factory1();
					foreach (var adapter1 in factory.Adapters1)
					{
						var devices = new IntPtr[10];
						clGetDeviceIDsFromD3D11KHR(
							platform.Handle.Value,
							cl_d3d11_device_source_khr.CL_D3D11_DXGI_ADAPTER_KHR,
							adapter1.NativePointer,
							cl_d3d11_device_set_khr.CL_ALL_DEVICES_FOR_D3D11_KHR,
							10,
							devices,
							out var numDevices);

						count += numDevices;
						if (numDevices > 0)
							Console.WriteLine($"\tAdapter {adapter1.Description1.Description} [{numDevices}]:");

						foreach (var deviceId in devices)
						{
							var oclDevice = platform.Devices.FirstOrDefault(x => x.Handle.Value == deviceId);
							if (oclDevice != null)
								Console.WriteLine($"\t\t{oclDevice.Name}");
						}

					}

					Console.WriteLine($"D3D11 KHR sharing [{count}]");
				}

				if (CL12x.TryFindClGetDeviceIDsFromD3D11NV(platform, out var clGetDeviceIDsFromD3D11NV))
				{
					var count = 0;
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

						count += numDevices;
						if (numDevices > 0)
							Console.WriteLine($"\tAdapter {adapter1.Description1.Description} [{numDevices}]:");

						foreach (var deviceId in devices)
						{
							var oclDevice = platform.Devices.FirstOrDefault(x => x.Handle.Value == deviceId);
							if (oclDevice != null)
								Console.WriteLine($"\t\t{oclDevice.Name}");
						}

					}

					Console.WriteLine($"D3D11 NV sharing [{count}]");
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

		private string Sum_kernel = @"
#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

void kernel sum(
	global uchar* a, 
	global uchar* b) 
{
	const int x = get_global_id(0);
    b[x] = a[x] * 2;
}
";

		[Test]
		public void Can_multiply()
		{
			var platform = ComputePlatform.Platforms.First();
			var device = platform.Devices.First();
			var context = new ComputeContext(new[] {device}, new ComputeContextPropertyList(platform), null, IntPtr.Zero);

			using var program = new ComputeProgram(context, Sum_kernel);

			program.Build(new[] { device }, "-cl-std=CL1.2", (handle, ptr) => OnProgramBuilt(program, device), IntPtr.Zero);

			using var queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);
			using var kernel = program.CreateKernel("sum");

			var source = new byte[] {1, 2, 3, 4, 5};
			using var sourceBuffer = context.CreateBuffer(source);

			var target = new byte[5];
			using var targetBuffer = context.CreateBuffer(target);

			kernel.SetMemoryArgument(0, sourceBuffer);
			kernel.SetMemoryArgument(1, targetBuffer);

			var errorCode = queue.Enqueue(kernel, new[] {source.Length});
			if (errorCode != ComputeErrorCode.Success)
			{
				Console.WriteLine(errorCode);
			}

			queue.Finish();

			for (var i = 0; i < source.Length; i++)
			{
				Console.WriteLine($"{source[i]} * 2 = {target[i]}");
			}
		}

		private void OnProgramBuilt(ComputeProgram program, ComputeDevice device)
		{
			var status = program.GetBuildStatus(device);
			if (status == ComputeProgramBuildStatus.Error)
			{
				var log = program.GetBuildLog(device);
				Console.WriteLine(log);
			}
		}

		private string Copy_kernel = @"
#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

constant sampler_t sampler = 
	CLK_NORMALIZED_COORDS_FALSE | 
	CLK_FILTER_NEAREST | 
	CLK_ADDRESS_CLAMP_TO_EDGE;

void kernel copy(
	read_only image2d_t a,
	global uchar* b) 
{
	//const int2 gid = { 0, 0 };
	const int2 gid = { get_global_id(0), get_global_id(1) };

	const int x = get_global_id(0);
    const int y = get_global_id(1);
	const int w = get_global_size(0);
    const int h = get_global_size(1);

	float4 p = read_imagef(a, sampler, gid);
	const int d = y * (w * 4) + (x * 4);
	
	//int d = 0;

	b[d+0] = p.x * 255;
	b[d+1] = p.y * 255;
	b[d+2] = p.z * 255;
	b[d+3] = p.w * 255;
}
";

		[Test]
		public void Can_copy()
		{
			var platform = ComputePlatform.Platforms.First();
			var device = platform.Devices.First();
			var context = new ComputeContext(new[] { device }, new ComputeContextPropertyList(platform), null, IntPtr.Zero);

			using var program = new ComputeProgram(context, Copy_kernel);

			try
			{
				program.Build(new[] {device}, "-cl-std=CL1.2", null, IntPtr.Zero);
			}
			catch (Exception ex)
			{
				OnProgramBuilt(program, device);
				return;
			}

			using var queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);
			using var kernel = program.CreateKernel("copy");

			var sourcePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sample00.png"));
			using var sourceImage = (Bitmap)Image.FromFile(sourcePath);

			var w = sourceImage.Width;
			var h = sourceImage.Height;

			var source = sourceImage.ToBytes();

			source.SaveFormatted("source.txt", w, h, bpp: 4);
			using var sourceBuffer = context.CreateImage2D(source, w, h);

			var target = new byte[h * w * 4];
			using var targetBuffer = context.CreateBuffer(target);

			kernel.SetMemoryArgument(0, sourceBuffer);
			kernel.SetMemoryArgument(1, targetBuffer);

			var errorCode = queue.Enqueue(kernel, new[] {w, h});
			if (errorCode != ComputeErrorCode.Success)
			{
				Console.WriteLine(errorCode);
			}

			queue.Finish();

			target.SaveFormatted("target.txt", w, h, bpp: 4);

			var result = target.ToBitmap(w, h, 4);
			var resultPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "copy.png"));
			result.Save(resultPath);

			Run("source.txt");
			Run("target.txt");
			Run(resultPath);
		}
	}
}