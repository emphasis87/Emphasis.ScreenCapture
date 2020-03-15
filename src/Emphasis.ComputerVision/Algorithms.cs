using System;
using System.Numerics;
using System.Text.RegularExpressions;
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
			for (var y = 0; y < height; y++)
			{
				var i = Clamp(y, height);
				for (var x = 0; x < width; x++)
				{
					var j = Clamp(x, width);
					for (var channel = 0; channel < 4; channel++)
					{
						var result =
							source[(i - 1) * (width * 4) + (j - 1) * 4 + channel] * 0.0625f +
							source[(i - 1) * (width * 4) + (j + 0) * 4 + channel] * 0.1250f +
							source[(i - 1) * (width * 4) + (j + 1) * 4 + channel] * 0.0625f +
							source[(i + 0) * (width * 4) + (j - 1) * 4 + channel] * 0.1250f +
							source[(i + 0) * (width * 4) + (j + 0) * 4 + channel] * 0.2500f +
							source[(i + 0) * (width * 4) + (j + 1) * 4 + channel] * 0.1250f +
							source[(i + 1) * (width * 4) + (j - 1) * 4 + channel] * 0.0625f +
							source[(i + 1) * (width * 4) + (j + 0) * 4 + channel] * 0.1250f +
							source[(i + 1) * (width * 4) + (j + 1) * 4 + channel] * 0.0625f;

						var d = y * (width * 4) + x * 4 + channel;
						destination[d] = Convert.ToByte(result);
					}
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
