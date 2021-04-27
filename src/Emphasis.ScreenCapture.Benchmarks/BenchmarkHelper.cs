using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Emphasis.ScreenCapture.Benchmarks
{
	public static class BenchmarkHelper
	{
		public static nuint Size<T>(int count) => (nuint)(Marshal.SizeOf<T>() * count);

		public static unsafe string GetString(byte* src, int size)
		{
			var srcSpan = new Span<byte>(src, size);
			var str = Encoding.ASCII.GetString(srcSpan.ToArray(), 0, size);
			return str;
		}
	}
}
