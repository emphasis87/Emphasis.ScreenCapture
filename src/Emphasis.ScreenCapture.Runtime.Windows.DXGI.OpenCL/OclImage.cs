using System;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI.OpenCL
{
	internal class OclImage : IOclImage
	{
		public nint ImageId { get; }
		public nint ImageWriteEventId { get; internal set; }
		public nint QueueId { get; }
		public bool IsAcquiringRequired { get; }

		private readonly OclPlatformInfo _platform;

		public OclImage(nint imageId)
		{
			ImageId = imageId;
		}

		public OclImage(
			nint imageId,
			nint queueId,
			OclPlatformInfo platform)
		{
			ImageId = imageId;
			QueueId = queueId;
			IsAcquiringRequired = true;
			_platform = platform;
		}

		public unsafe void AcquireObject(nint[] waitEventIds, out nint eventId)
		{
			var eventsCount = waitEventIds?.Length ?? 0;
			var eventIds = stackalloc nint[eventsCount];
			waitEventIds?.CopyTo(new Span<nint>(eventIds, eventsCount));

			var imageId = ImageId;

			nint eventId2;
			var err = _platform.EnqueueAcquireD3D11Objects(QueueId, 1, &imageId, (uint)eventsCount, eventIds, &eventId2);
			if (err != 0)
				throw new ScreenCaptureException("Unable to acquire D3D11 object.");

			eventId = eventId2;
		}

		public unsafe void ReleaseObject(nint[] waitEventIds, out nint eventId)
		{
			var eventsCount = waitEventIds?.Length ?? 0;
			var eventIds = stackalloc nint[eventsCount];
			waitEventIds?.CopyTo(new Span<nint>(eventIds, eventsCount));

			var imageId = ImageId;
			nint eventId2;
			var err = _platform.EnqueueReleaseD3D11Objects(QueueId, 1, &imageId, (uint)eventsCount, eventIds, &eventId2);
			if (err != 0)
				throw new ScreenCaptureException("Unable to release D3D11 object.");

			eventId = eventId2;
		}
	}
}