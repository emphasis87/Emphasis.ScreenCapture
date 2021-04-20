using System;
using System.Collections.Generic;
using System.Text;

namespace Emphasis.ScreenCapture
{
	public interface IScreenProvider
	{
		IScreen[] GetScreens();
	}
}
