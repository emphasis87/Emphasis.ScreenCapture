#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

constant float gauss[3][3] = 
{   
	{ 0.0625, 0.1250, 0.0625 },
	{ 0.1250, 0.2500, 0.1250 },
    { 0.0625, 0.1250, 0.0625 },
};

void kernel gauss_blur_u8(
	global uchar* in_grayscale,
	global uchar* out_blur) 
{
	const int x = get_global_id(0);
    const int y = get_global_id(1);
	const int w = get_global_size(0);
    const int h = get_global_size(1);
	const int d = y * w + x;

	if (y == 0 || y == h - 1 || x == 0 || x == w - 1)
	{
		out_blur[d] = in_grayscale[d];
	}
	else 
	{
		// x = max(1, min(x, w-1));
		// y = max(1, min(y, h-1));
		
		const int sum = 
			gauss[0][0] * in_grayscale[(y -1) * w + (x -1)] +
			gauss[0][1] * in_grayscale[(y -1) * w + (x +0)] +
			gauss[0][2] * in_grayscale[(y -1) * w + (x +1)] +
			gauss[1][0] * in_grayscale[(y +0) * w + (x -1)] +
			gauss[1][1] * in_grayscale[(y +0) * w + (x +0)] +
			gauss[1][2] * in_grayscale[(y +0) * w + (x +1)] +
			gauss[2][0] * in_grayscale[(y +1) * w + (x -1)] +
			gauss[2][1] * in_grayscale[(y +1) * w + (x +0)] +
			gauss[2][2] * in_grayscale[(y +1) * w + (x +1)];
		
		out_blur[d] = convert_uchar_sat(sum);
	}
}
