using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Emphasis.ScreenCapture.Tests
{
	public static class TestHelper
	{
		public static void Run(Bitmap bitmap, string name)
		{
			if (!Path.HasExtension(name))
				name = $"{name}.png";

			bitmap.Save(name);
			Run(name);
		}

		public static void Run(string path, string arguments = null)
		{
			if (!Path.IsPathRooted(path) && Path.HasExtension(path))
				path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

			var info = new ProcessStartInfo(path)
			{
				UseShellExecute = true
			};
			if (arguments != null)
			{
				info.Arguments = arguments;
			}

			Process.Start(info);
		}

		public static nuint Size<T>(int count) => (nuint)(Marshal.SizeOf<T>() * count);

		public static unsafe string GetString(byte* src, int size)
		{
			var srcSpan = new Span<byte>(src, size);
			var str = Encoding.ASCII.GetString(srcSpan.ToArray(), 0, size);
			return str;
		}
	}
}
