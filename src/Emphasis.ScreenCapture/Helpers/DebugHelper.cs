using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace Emphasis.ScreenCapture.Helpers
{
	public static class DebugHelper
	{
		public static IEnumerable<string> PrintFormatted(this byte[] data, int width, int height, int bpp = 1)
		{
			var sb = new StringBuilder();
			for (var y = 0; y < height; y++)
			{
				var line = data.AsSpan(y * width * bpp, width * bpp);
				for (var x = 0; x < width; x++)
				{
					var pixel = line.Slice(x * bpp, bpp);
					for (var i = 0; i < bpp; i++)
					{
						sb.Append($"{pixel[i],3} ");
					}

					sb.Append("| ");
				}

				yield return sb.ToString();

				sb.Clear();
			}
		}

		public static void SaveFormatted(this byte[] data, string path, int width, int height, int bpp = 1)
		{
			using var stream = new StreamWriter(path, false, Encoding.UTF8);
			foreach (var line in PrintFormatted(data, width, height, bpp))
			{
				stream.WriteLine(line);
			}
		}

		public static void Run(string path)
		{
			if (!Path.IsPathRooted(path))
				Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

			var info = new ProcessStartInfo(path)
			{
				UseShellExecute = true
			};
			Process.Start(info);
		}

		public static void RunAs(this Bitmap image, string filename)
		{
			var path =
				Path.GetFullPath(
					Path.Combine(
						Environment.CurrentDirectory,
						filename));
			image.Save(path);
			Run(path);
		}

		public static void RunAs(this byte[] image, int width, int height, int bpp, string filename)
		{
			using var bitmap = image.ToBitmap(width, height, bpp);
			RunAs(bitmap, filename);
		}
	}
}
