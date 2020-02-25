using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Cloo;
using Cloo.Bindings;
using Emphasis.OpenCL;
using NUnit.Framework;
using SharpDX;
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
			var sourceBuffer = new ComputeBuffer<byte>(
				context,
				ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer,
				source);

			var target = new byte[5];
			var targetBuffer = new ComputeBuffer<byte>(
				context,
				ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer,
				target);

			kernel.SetMemoryArgument(0, sourceBuffer);
			kernel.SetMemoryArgument(1, targetBuffer);

			var globalWorkSize = new[] { (IntPtr)source.Length };
			var errorCode = CL10.EnqueueNDRangeKernel(
				queue.Handle,
				kernel.Handle,
				globalWorkSize.Length,
				null,
				globalWorkSize,
				null,
				0,
				null,
				null);

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

		public byte[] ToRawArgb(Bitmap image)
		{
			var w = image.Width;
			var h = image.Height;
			var bounds = new System.Drawing.Rectangle(0, 0, w, h);
			var data = image.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			var result = new byte[h * w * 4];
			var resultHandler = GCHandle.Alloc(result, GCHandleType.Pinned);
			var resultPointer = resultHandler.AddrOfPinnedObject();

			var sourcePointer = data.Scan0;
			for (var y = 0; y < h; y++)
			{
				Utilities.CopyMemory(resultPointer, sourcePointer, w * 4);

				sourcePointer = IntPtr.Add(sourcePointer, data.Stride);
				resultPointer = IntPtr.Add(resultPointer, w * 4);
			}
			
			image.UnlockBits(data);

			resultHandler.Free();

			return result;
		}

		public string Print(byte[] data, int w, int h)
		{
			var sb = new StringBuilder();
			var source = data.AsSpan();
			for (var y = 0; y < h; y++)
			{
				var line = source.Slice(y * w * 4, w * 4);
				for (var x = 0; x < w; x++)
				{
					var pixel = line.Slice(x * 4, 4);
					for (var i = 0; i < 4; i++)
					{
						sb.Append($"{pixel[i],3} ");
					}

					sb.Append(", ");
				}

				sb.AppendLine();
			}

			return sb.ToString();
		}


		[Test]
		public void Can_copy()
		{
			var platform = ComputePlatform.Platforms.First();
			var device = platform.Devices.First();
			var context = new ComputeContext(new[] { device }, new ComputeContextPropertyList(platform), null, IntPtr.Zero);

			using var program = new ComputeProgram(context, Copy_kernel);

			try
			{
				program.Build(new[] {device}, "-cl-std=CL1.2", (handle, ptr) => OnProgramBuilt(program, device),
					IntPtr.Zero);
			}
			catch (Exception ex)
			{
				OnProgramBuilt(program, device);
				return;
			}

			using var queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);
			using var kernel = program.CreateKernel("copy");

			var sourcePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "sample00.png"));
			var sourceImage = (Bitmap)Image.FromFile(sourcePath);

			var w = sourceImage.Width;
			var h = sourceImage.Height;

			var source = ToRawArgb(sourceImage);

			File.WriteAllText("source.txt", Print(source, w, h));

			var sourceHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
			var sourcePointer = sourceHandle.AddrOfPinnedObject();

#pragma warning disable CS0618 // Type or member is obsolete
			var sourceBuffer = new ComputeImage2D(
				context,
				ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer,
				new ComputeImageFormat(ComputeImageChannelOrder.Bgra, ComputeImageChannelType.UNormInt8),
				w,
				h,
				0,
				sourcePointer);
#pragma warning restore CS0618 // Type or member is obsolete

			var target = new byte[h * w * 4];
			var targetBuffer = new ComputeBuffer<byte>(
				context,
				ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer,
				target);

			kernel.SetMemoryArgument(0, sourceBuffer);
			kernel.SetMemoryArgument(1, targetBuffer);

			var globalWorkSize = new[] {(IntPtr) w, (IntPtr) h};
			var errorCode = CL10.EnqueueNDRangeKernel(
				queue.Handle,
				kernel.Handle,
				globalWorkSize.Length,
				null,
				globalWorkSize,
				null,
				0,
				null,
				null);

			if (errorCode != ComputeErrorCode.Success)
			{
				Console.WriteLine(errorCode);
			}

			queue.Finish();

			File.WriteAllText("target.txt", Print(target, w, h));

			var targetHandle = GCHandle.Alloc(target, GCHandleType.Pinned);
			var targetPointer = targetHandle.AddrOfPinnedObject();

			var result = new Bitmap(w, h);

			var bounds = new System.Drawing.Rectangle(0, 0, w, h);
			var resultData = result.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			var resultPointer = resultData.Scan0;

			for (var y = 0; y < h; y++)
			{
				Utilities.CopyMemory(resultPointer, targetPointer, w * 4);

				targetPointer = IntPtr.Add(targetPointer, w * 4);
				resultPointer = IntPtr.Add(resultPointer, w * 4);
			}

			result.UnlockBits(resultData);

			var resultPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "copy.png"));
			result.Save(resultPath);

			sourceHandle.Free();
			targetHandle.Free();

			// Display the texture using system associated viewer

			System.Diagnostics.Process.Start(
				new ProcessStartInfo("source.txt")
				{
					UseShellExecute = true
				});

			System.Diagnostics.Process.Start(
				new ProcessStartInfo("target.txt")
				{
					UseShellExecute = true
				});

			System.Diagnostics.Process.Start(
				new ProcessStartInfo(resultPath)
				{
					UseShellExecute = true
				});
		}
	}
}