using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Cloo;

namespace Emphasis.OpenCL.Helpers
{
	public static class BufferHelper
	{
		public static ManagedComputeImage2D CreateImage2D(this ComputeContext context, byte[] data, int width, int height)
		{
			var sourceHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var sourcePointer = sourceHandle.AddrOfPinnedObject();

			var image = CreateImage2D(context, sourcePointer, width, height);
			image.Add(Disposable.Create(() => sourceHandle.Free()));

			return image;
		}

		public static ManagedComputeImage2D CreateImage2D(this ComputeContext context, IntPtr dataPointer, int width, int height)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			var image = new ManagedComputeImage2D(
				context,
				ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer,
				new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.UNormInt8),
				width,
				height,
				0,
				dataPointer);
#pragma warning restore CS0618 // Type or member is obsolete
			return image;
		}

		public static ComputeBuffer<byte> CreateBuffer(this ComputeContext context, byte[] data)
		{
			var buffer = new ComputeBuffer<byte>(
				context,
				ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer,
				data);

			return buffer;
		}
	}
}
