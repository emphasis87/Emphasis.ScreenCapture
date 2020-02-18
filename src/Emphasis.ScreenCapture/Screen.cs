using System;
using System.Diagnostics.CodeAnalysis;
using SharpDX.DXGI;

namespace Emphasis.ScreenCapture
{
	public class Screen : IEquatable<Screen>
	{
		public Adapter1 Adapter { get; }
		public int AdapterId => Adapter.Description.DeviceId;

		public Output1 Output { get; }
		public string OutputName => Output.Description.DeviceName;

		public Screen(
			[NotNull]Adapter1 adapter,
			[NotNull]Output1 output)
		{
			Adapter = adapter;
			Output = output;
		}

		public bool Equals(Screen other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return AdapterId.Equals(other.AdapterId) && OutputName.Equals(other.OutputName);
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Screen) obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(AdapterId, OutputName);
		}
	}
}