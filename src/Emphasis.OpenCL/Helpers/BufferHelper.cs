using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Cloo;

namespace Emphasis.OpenCL.Helpers
{
	public static class BufferHelper
	{
		public static ComputeImage2D CreateImage2D(this ComputeContext context, byte[] data, int width, int height)
		{
			var sourceHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var sourcePointer = sourceHandle.AddrOfPinnedObject();

#pragma warning disable CS0618 // Type or member is obsolete
			var image = new ManagedComputeImage2D(
				context,
				ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.UseHostPointer,
				new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.UNormInt8),
				width,
				height,
				0,
				sourcePointer);
#pragma warning restore CS0618 // Type or member is obsolete

			image.Add(
				Disposable.Create(() => sourceHandle.Free()));

			return image;
		}
	}
}
