using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Emphasis.ComputerVision;

namespace Emphasis.ScreenCapture.Helpers
{
	public static class DebugHelper
	{
		public static IEnumerable<string> PrintFormatted(this byte[] data, int width, int height, int channels = 1)
		{
			var sb = new StringBuilder();
			for (var y = 0; y < height; y++)
			{
				var line = data.AsSpan(y * width * channels, width * channels);
				for (var x = 0; x < width; x++)
				{
					var pixel = line.Slice(x * channels, channels);
					for (var i = 0; i < channels; i++)
					{
						sb.Append($"{pixel[i],4} ");
					}

					sb.Append("| ");
				}

				yield return sb.ToString();

				sb.Clear();
			}
		}

		public static IEnumerable<string> PrintFormatted(this float[] data, int width, int height, int channels = 1, bool rounded = true)
		{
			var sb = new StringBuilder();
			for (var y = 0; y < height; y++)
			{
				var line = data.AsSpan(y * width * channels, width * channels);
				for (var x = 0; x < width; x++)
				{
					var pixel = line.Slice(x * channels, channels);
					for (var i = 0; i < channels; i++)
					{
						var v = pixel[i];
						if (rounded)
							v = (float)Math.Round(v);
						sb.Append($"{v,4} ");
					}

					sb.Append("| ");
				}

				yield return sb.ToString();

				sb.Clear();
			}
		}

		public static IEnumerable<string> PrintFormatted(this int[] data, int width, int height, int channels = 1)
		{
			var min = int.MaxValue;
			var max = int.MinValue;
			for (var i = 0; i < data.Length; i++)
			{
				var v = data[i];
				min = Math.Min(min, v);
				max = Math.Max(max, v);
			}

			var len0 = min.ToString().Length;
			var len1 = max.ToString().Length;
			var len = Math.Max(len0, len1) + 1;

			var sb = new StringBuilder();
			for (var y = 0; y < height; y++)
			{
				var line = data.AsSpan(y * width * channels, width * channels);
				for (var x = 0; x < width; x++)
				{
					var pixel = line.Slice(x * channels, channels);
					sb.Append("|");
					for (var i = 0; i < channels; i++)
					{
						var v = $"{pixel[i]}".PadLeft(len);
						sb.Append($"{v} ");
					}
				}

				yield return sb.ToString();

				sb.Clear();
			}
		}

		public static void SaveFormatted(this byte[] data, string path, int width, int height, int channels = 1)
		{
			using var stream = new StreamWriter(path, false, Encoding.UTF8);
			foreach (var line in PrintFormatted(data, width, height, channels))
			{
				stream.WriteLine(line);
			}
		}

		public static void SaveFormatted(this float[] data, string path, int width, int height, int channels = 1, bool rounded = true)
		{
			using var stream = new StreamWriter(path, false, Encoding.UTF8);
			foreach (var line in PrintFormatted(data, width, height, channels, rounded))
			{
				stream.WriteLine(line);
			}
		}

		public static void SaveFormatted(this int[] data, string path, int width, int height, int channels = 1)
		{
			using var stream = new StreamWriter(path, false, Encoding.UTF8);
			foreach (var line in PrintFormatted(data, width, height, channels))
			{
				stream.WriteLine(line);
			}
		}

		public static void RunAsText(this byte[] data, int width, int height, int channels, string path)
		{
			if (!Path.IsPathRooted(path) && Path.HasExtension(path))
				path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

			SaveFormatted(data, path, width, height, channels);
			Run("code", path);
		}

		public static void RunAsText(this float[] data, int width, int height, int channels, string path, bool rounded = true)
		{
			if (!Path.IsPathRooted(path) && Path.HasExtension(path))
				path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

			SaveFormatted(data, path, width, height, channels, rounded);
			Run("code", path);
		}

		public static void RunAsText(this int[] data, int width, int height, int channels, string path)
		{
			if (!Path.IsPathRooted(path) && Path.HasExtension(path))
				path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

			SaveFormatted(data, path, width, height, channels);
			Run("code", path);
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

		public static void RunAs(this byte[] image, int width, int height, int channels, string filename)
		{
			using var bitmap = image.ToBitmap(width, height, channels);
			RunAs(bitmap, filename);
		}

		public static void RunAs(this float[] image, int width, int height, int channels, string filename)
		{
			var normalized = new byte[image.Length];
			image.Normalize(normalized, channels);
			using var bitmap = normalized.ToBitmap(width, height, channels);
			RunAs(bitmap, filename);
		}

		public static void RunAs(this int[] image, int width, int height, int channels, string filename)
		{
			var normalized = new byte[image.Length];
			image.Normalize(normalized, channels);
			using var bitmap = normalized.ToBitmap(width, height, channels);
			RunAs(bitmap, filename);
		}

		public static void Dump(this int[] data, int width, int height)
		{
			Console.WriteLine("{");
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					Console.Write($"{data[d]}, ");
				}
				Console.WriteLine();
			}
			Console.WriteLine("}");
		}
	}
}
