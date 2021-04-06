﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Emphasis.ComputerVision
{
	public class Matrix<T>
	{
		public int Width;
		public int Height;
		public int Channels;
		public Memory<T> Data;
	}
}
