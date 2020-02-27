using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Emphasis.ScreenCapture.Helpers
{
	public static class DebugHelper
	{
		public static string Print(this byte[] data, int width, int height)
		{
			var sb = new StringBuilder();
			var source = data.AsSpan();
			for (var y = 0; y < height; y++)
			{
				var line = source.Slice(y * width * 4, width * 4);
				for (var x = 0; x < width; x++)
				{
					var pixel = line.Slice(x * 4, 4);
					for (var i = 0; i < 4; i++)
					{
						sb.Append($"{pixel[i],3} ");
					}

					sb.Append("| ");
				}

				sb.AppendLine();
			}

			return sb.ToString();
		}

		public static void SaveToFile(this byte[] data, string path, int width, int height)
		{
			File.WriteAllText(path, Print(data, width, height));
		}

		public static void Run(string path)
		{
			var info = new ProcessStartInfo(path)
			{
				UseShellExecute = true
			};
			Process.Start(info);
		}
	}
}
