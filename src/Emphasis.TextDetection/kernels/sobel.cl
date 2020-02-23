#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

void kernel sobel_u8(
	global uchar* in_gray,
	global uchar* out_sobel_x,
	global uchar* out_sobel_y,
	global uchar* out_sobel_gradient,
	global uchar* out_sobel_angle) 
{
	int x = get_global_id(0);
    int y = get_global_id(1);
	int w = get_global_size(0);
    int h = get_global_size(1);

	if (x == 0 || x == w-1 || y == 0 || y == h-1)
		return;

	int d = y * w + x;

	uchar i00 = in_gray[(x -1) + (y -1) * w];
	uchar i01 = in_gray[(x   ) + (y -1) * w];
	uchar i02 = in_gray[(x +1) + (y -1) * w];
	uchar i10 = in_gray[(x -1) + (y   ) * w];
	uchar i12 = in_gray[(x +1) + (y   ) * w];
	uchar i20 = in_gray[(x -1) + (y +1) * w];
	uchar i21 = in_gray[(x   ) + (y +1) * w];
	uchar i22 = in_gray[(x +1) + (y +1) * w];

	float sum_x = 
		+ (-1 * i00) + (+1 * i02) 
		+ (-2 * i10) + (+2 * i12) 
		+ (-1 * i20) + (+1 * i22);

	float sum_y = 
		+ (-i00) + (-2 * i01) + (-i02)
		+ (+i20) + (+2 * i21) + (+i22);
		
	// hypot(x,y) = sqrt(x*x + y*y)
	float gradient = hypot(sum_x, sum_y);

	// atan2pi(y,x) = atan2(y,x) / PI; 4-quadrant angle in range (-1;1] 
	float angle = atan2pi(sum_y, sum_x);

	out_sobel_x[d] = convert_uchar_sat(sum_x);
	out_sobel_y[d] = convert_uchar_sat(sum_y);

	out_sobel_gradient[d] = convert_uchar_sat(gradient);

	float direction = (angle  + 1.125) * 4 - 1;
	out_sobel_angle[d] = convert_uchar_rtz(direction);
}
