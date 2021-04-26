namespace Emphasis.ScreenCapture
{
	public interface IOclImage
	{
		nint ImageId { get; }
		bool IsAcquiringRequired { get; }

		void AcquireObject(nint[] waitEventIds, out nint eventId);
		void ReleaseObject(nint[] waitEventIds, out nint eventId);
	}
}
