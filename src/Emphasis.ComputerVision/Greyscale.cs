using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Emphasis.ComputerVision
{
	public partial class Algorithms
	{
		//constant float gauss[3][3] = 
		//{   
		//	{ 0.0625, 0.1250, 0.0625 },
		//	{ 0.1250, 0.2500, 0.1250 },
		//	{ 0.0625, 0.1250, 0.0625 },
		//};

		public static void Gauss(int width, int height, byte[] source, byte[] destination)
		{
			var simd = Vector<byte>.Count;
			Span<byte> a = stackalloc byte[simd];
			
			var v= new Vector<byte>(a);
			Vector.Widen(v, out var d1, out var d2);

			for (var y = 0; y < height; y++)
			{
				var i = Clamp(y, height);
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					var j = Clamp(x, width);


				}
			}
		}

		public static void Sobel(int width, int height, byte[] source, byte[] destination)
		{
			for (var y = 0; y < height; y++)
			{
				var i = Clamp(y, height);
				for (var x = 0; x < width; x++)
				{
					var d = y * width + x;
					var j = Clamp(x, width);


				}
			}
		}

		private static int Clamp(int value, int max)
		{
			if (value == 0)
				return 1;
			if (value == max - 1)
				return max - 2;
			return value;
		}

	}
}
