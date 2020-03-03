#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

constant sampler_t sampler = 
	CLK_NORMALIZED_COORDS_FALSE | 
	CLK_FILTER_NEAREST | 
	CLK_ADDRESS_CLAMP_TO_EDGE;

constant int sobx[3][3] = 
{
	{-1, 0, 1},
	{-2, 0, 2},
	{-1, 0, 1},
};

constant int soby[3][3] = 
{
	{-1,-2,-1},
	{ 0, 0, 0},
	{ 1, 2, 1},
};

constant int scharrx[3][3] = 
{
	{-3,  0, 3 },
	{-10, 0, 10},
	{-3,  0, 3 },
};

constant int scharry[3][3] = 
{
	{-3,  -10, -3 },
	{ 0,    0,  0 },
	{ 3,   10,  3 },
};

constant float PI = 3.14159265;

//constant float4 gray_mask = { 0.2989f, 0.5870f, 0.1140f, 0 };
constant float4 gray_mask = { 0.2126f, 0.7152f, 0.0722f, 0 };

// src: input image
// gray: after grayscale conversion
// gradient: intensity gradient (the length of the hypotenuse of vertical and horizontal intensity derivatives)
// angle: angle of the intensity gradient in (-PI, PI)
// theta: angle rounded to 0°, 45°, 90°, 135°
void kernel canny(
	read_only image2d_t src,
	global float* gray, 
	global float* gradient,
	global uchar* dst) 
{
	int x = get_global_id(0);
    int y = get_global_id(1);
	int w = get_global_size(0);
    int h = get_global_size(1);
	int d = x + y * w;

	// The intermediate results are extended for convolution
	int w2 = w + 2;
	int h2 = h + 2;
	int x2 = x + 1;
	int y2 = y + 1;
	int d2 = x2 + y2 * w2;

	int2 gid = { get_global_id(0), get_global_id(1) };
	int2 lid = { get_local_id(0), get_local_id(1) };
	int2 size = { get_image_width(src), get_image_height(src) };

	int pin = gid.x + gid.y * size.x;
	int pout = (gid.x + 1) + (gid.y + 1) * (size.x + 2);

	if (!all(gid < size))
		return;

	// read the image values as in range [0.0, 1.0]
	float4 p = read_imagef(src, sampler, gid);
	
	float g0 = dot(p, gray_mask) * 255;
	//uchar g1 = convert_uchar_sat(g0);

	gray[d2] = g0;

	if (x == 0)
	{ 
		if (y == 0)
			gray[0] = g0;
		gray[d2 - 1] = g0;
	} 
	else if (x == w - 1)
	{ 
		if (y == 0)
			gray[d2 + 1 - w2] = g0;
		gray[d2 + 1] = g0;
	}

	barrier(CLK_GLOBAL_MEM_FENCE);

	float a[3][3] = 
	{
		{ 
			gray[(x2 -1) + (y2 -1) * w2],
			gray[(x2   ) + (y2 -1) * w2],
			gray[(x2 +1) + (y2 -1) * w2],
		},
		{ 
			gray[(x2 -1) + (y2   ) * w2],
			gray[(x2   ) + (y2   ) * w2],
			gray[(x2 +1) + (y2   ) * w2],
		},
		{ 
			gray[(x2 -1) + (y2 +1) * w2],
			gray[(x2   ) + (y2 +1) * w2],
			gray[(x2 +1) + (y2 +1) * w2],
		},
	};

	// Find x and y 1st derivatives
	float sumx = 0;
	float sumy = 0;

	for (int row = 0; row < 3; row++)
    {
        for (int col = 0; col < 3; col++)
        {
			int xn = x2 + col - 1;
			int yn = y2 + row - 1;
			int index = xn + yn * w2;

			sumx += a[row][col] * sobx[row][col];
			sumy += a[row][col] * soby[row][col];

            //sumx += gray[index] * sobx[row][col];
            //sumy += gray[index] * soby[row][col];
        }
    }

	float grad = hypot(sumx, sumy);

	/*
	
	uchar grad_uc = min(255, max(0, (int)grad));
	gradient[pout] = grad;
	*/

	//dst[pout] = g0; //convert_uchar_sat(sumy); // g0; //grad_uc;// convert_uchar_sat(sumx);

	dst[d2] = convert_uchar_sat(sumy);
}
