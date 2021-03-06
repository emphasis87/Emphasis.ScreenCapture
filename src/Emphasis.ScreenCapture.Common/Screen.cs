﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace Emphasis.ScreenCapture
{
	public interface IScreen : IEquatable<IScreen>
	{
		int AdapterId { get; }
		string OutputName { get; }
	}

	public class Screen : IScreen
	{
		/// <summary>
		/// DeviceId of the selected adapter
		/// </summary>
		public int AdapterId { get; }

		/// <summary>
		/// DeviceName of the selected output
		/// </summary>
		public string OutputName { get; }

		public Screen(int adapterId, [NotNull]string outputName)
		{
			AdapterId = adapterId;
			OutputName = outputName;
		}

		#region Equals
		public bool Equals(IScreen other)
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
		#endregion
	}
}