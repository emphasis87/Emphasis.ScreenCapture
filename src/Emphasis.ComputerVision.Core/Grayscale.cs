using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Emphasis.ComputerVision.Core
{
	public partial class Algorithms
	{
		private static readonly Vector4 Weights = new Vector4(0.2126f, 0.7152f, 0.0722f, 0);
		
		public static async Task Grayscale(Matrix<byte> input, Matrix<byte> output, Vector4 weights = default, int parallelismLevel = 0)
		{
			if (input.Channels != 4)
				throw new ArgumentOutOfRangeException(nameof(input), "Only 4 channel input is supported.");
			if (output.Channels != 1)
				throw new ArgumentOutOfRangeException(nameof(input), "Only 1 channel output is supported.");
			if (input.Width != output.Width || input.Height != output.Height)
				throw new ArgumentOutOfRangeException(nameof(input), "Input and output have to have the same dimensions.");
			
			if (weights == default)
				weights = Weights;
			if (parallelismLevel < 1 || parallelismLevel > Environment.ProcessorCount)
				parallelismLevel = Environment.ProcessorCount;

			var h = input.Height;
			var w = input.Width;

			var ySlice = h / parallelismLevel;
			var tasks = new List<Task>();
			for (var i = 0; i < parallelismLevel; i++)
			{
				var i0 = i;
				var task = Task.Run(() => Execute(i0));
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			void Execute(int i)
			{
				var a = input.Data.Span;
				var b = output.Data.Span;
				
				var y0 = i * ySlice;
				var ym = i + 1 < parallelismLevel ? (i + 1) * ySlice : h;

				var id = y0 * w * 4;
				var od = y0 * w;
				
				for (var y = y0; y < ym; y++)
				{
					for (var x = 0; x < w; x++)
					{
						var p = new Vector4(a[id], a[id + 1], a[id + 2], a[id + 3]);
						var g = Vector4.Dot(p, weights);
						b[od] = Convert.ToByte(Math.Min(g, 255));
						id += 4;
						od += 1;
					}
				}
			}
		}
	}
}