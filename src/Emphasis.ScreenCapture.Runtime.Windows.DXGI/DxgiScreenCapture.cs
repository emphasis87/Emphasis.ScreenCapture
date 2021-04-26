using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Emphasis.ScreenCapture.Runtime.Windows.DXGI
{
	public class DxgiScreenCapture : ScreenCapture
	{
		internal DxgiScreenCaptureSharedResources SharedResources { get; }

		public Adapter1 Adapter => SharedResources.Adapter;
		public Output1 Output => SharedResources.Output1;
		public Device Device => SharedResources.Device;
		public OutputDuplication OutputDuplication => SharedResources.OutputDuplication;

		public Resource ScreenResource { get; }
		public OutputDuplicateFrameInformation FrameInformation { get; }
		
		private readonly IDisposable _disposable;

		internal DxgiScreenCapture(
			[NotNull] IScreen screen,
			[NotNull] IServiceProvider serviceProvider,
			DateTime time,
			int width,
			int height,
			[NotNull] DxgiScreenCaptureSharedResources resources,
			[NotNull] Resource screenResource,
			[NotNull] OutputDuplicateFrameInformation frameInformation)
			: base(screen, serviceProvider, time, width, height)
		{
			SharedResources = resources;
			ScreenResource = screenResource;
			FrameInformation = frameInformation;
			
			_disposable = Disposable.Create(() =>
			{
				ScreenResource.Dispose();
				OutputDuplication.ReleaseFrame();
				SharedResources.RemoveReference();
			});

			SharedResources.AddReference();
		}

		public override void Dispose()
		{
			base.Dispose();
			
			_disposable.Dispose();
		}
	}
}