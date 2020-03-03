using System;
using Cloo;
using Emphasis.OpenCL;

namespace Emphasis.TextDetection
{
	public class TextDetector
	{
		private readonly IComputeManager _computeManager;

		public TextDetector(IComputeManager computeManager)
		{
			_computeManager = computeManager;
		}

		public void DetectText(ComputeContext context, ComputeImage2D image, int width, int height)
		{

		}
	}
}
