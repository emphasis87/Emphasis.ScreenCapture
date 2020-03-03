using System.Collections;
using System.Globalization;
using Emphasis.OpenCL;

namespace Emphasis.TextDetection
{
	public class TextDetectionKernels
	{
		private readonly IComputeManager _computeManager;

		public TextDetectionKernels(IComputeManager computeManager)
		{
			const string options = "-cl-std=CL1.2";
			computeManager.AddProgram(KernelSources.grayscale, options);
			computeManager.AddProgram(KernelSources.sobel, options);
			computeManager.AddProgram(KernelSources.canny, options);
			computeManager.AddProgram(KernelSources.non_maximum_supression, options);

			_computeManager = computeManager;
		}
	}
}