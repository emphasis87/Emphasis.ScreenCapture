#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

constant sampler_t sampler = 
	CLK_NORMALIZED_COORDS_FALSE | 
	CLK_FILTER_NEAREST | 
	CLK_ADDRESS_CLAMP_TO_EDGE; 

constant float4 m1 = { -1, -2, -1, 0 };
constant float4 m2 = { +1, +2, +1, 0 };

float sum(float4 v){
	return v.s1 + v.y + v.z + v.w;
}

void gradient_u8(
	global uchar* out_gradient,
	global uchar* out_direction,
	int d,
	float dx,
	float dy)
{
	// Find the gradient (value of the change in color)
	// hypot(x,y) = sqrt(x*x + y*y)
	float gradient = hypot(dx, dy);

	// Find angle of the gradient change (i.e. perpendicular to the edge)
	// atan2pi(y,x) = atan2(y,x) / PI; 4-quadrant angle in range (-1;1] 
	float angle = atan2pi(dy, dx);

	out_gradient[d] = convert_uchar_sat(gradient);

	// Convert the angle into 8 distinct directions
	float direction = (angle + 1.125) * 4 - 1;
	uchar direction_u8 = convert_uchar_rtz(direction);
	// Indexes 0,8,9 denote the same direction
	if (direction_u8 > 7)
		direction_u8 = 0;
	
	out_direction[d] = direction_u8;
}

void kernel sobel(
	read_only image2d_t in_image,
	global uchar* out_sobel_gradient,
	global uchar* out_sobel_direction) 
{
	const int x = get_global_id(0);
	const int y = get_global_id(1);
	const int w = get_image_width(in_image);
	const int h = get_image_height(in_image);
	const int d = y*w + x;

	// Read the image values as in range [0.0, 1.0]
	const float4 p00 = read_imagef(in_image, sampler, (int2)(x-1, y-1));
	const float4 p01 = read_imagef(in_image, sampler, (int2)(x+0, y-1));
	const float4 p02 = read_imagef(in_image, sampler, (int2)(x+1, y-1));
	const float4 p10 = read_imagef(in_image, sampler, (int2)(x-1, y+0));
	const float4 p11 = read_imagef(in_image, sampler, (int2)(x+0, y+0));
	const float4 p12 = read_imagef(in_image, sampler, (int2)(x+1, y+0));
	const float4 p20 = read_imagef(in_image, sampler, (int2)(x-1, y+1));
	const float4 p21 = read_imagef(in_image, sampler, (int2)(x+0, y+1));
	const float4 p22 = read_imagef(in_image, sampler, (int2)(x+1, y+1));

	const float sum_x0 = 
		dot(m1, (float4)(p00.x, p10.x, p20.x, 0)) +
		dot(m2, (float4)(p02.x, p12.x, p22.x, 0));
	const float sum_x1 =
		dot(m1, (float3)(p00.y, p10.y, p20.y, 0)) +
		dot(m2, (float3)(p02.y, p12.y, p22.y, 0));
	const float sum_x2 =
		dot(m1, (float3)(p00.z, p10.z, p20.z, 0)) +
		dot(m2, (float3)(p02.z, p12.z, p22.z, 0));
	const float sum_x3 =
		dot(m1, (float3)(p00.w, p10.w, p20.w, 0)) +
		dot(m2, (float3)(p02.w, p12.w, p22.w, 0));

	const float sum_y0 =
		dot(m1, (float3)(p00.x, p01.x, p02.x, 0)) +
		dot(m2, (float3)(p20.x, p21.x, p22.x, 0));
	const float sum_y1 =
		dot(m1, (float3)(p00.y, p01.y, p02.y, 0)) +
		dot(m2, (float3)(p20.y, p21.y, p22.y, 0));
	const float sum_y2 =
		dot(m1, (float3)(p00.z, p01.z, p02.z, 0)) +
		dot(m2, (float3)(p20.z, p21.z, p22.z, 0));
	const float sum_y3 =
		dot(m1, (float3)(p00.w, p01.w, p02.w, 0)) +
		dot(m2, (float3)(p20.w, p21.w, p22.w, 0));
	
	const float sum_x = maxmag(maxmag(maxmag(sum_x0, sum_x1), sum_x2), sum_x3);
	const float sum_y = maxmag(maxmag(maxmag(sum_y0, sum_y1), sum_y2), sum_y3);

	gradient_u8(out_sobel_gradient, out_sobel_direction, d, sum_x, sum_y);
}

void kernel sobel_u8(
	global uchar* in_gray,
	global uchar* out_sobel_gradient,
	global uchar* out_sobel_direction) 
{
	const int x = get_global_id(0);
    const int y = get_global_id(1);
	const int w = get_global_size(0);
    const int h = get_global_size(1);

	if (x == 0 || x == w-1 || y == 0 || y == h-1)
		return;

	const int d = y * w + x;

	const uchar i00 = in_gray[(x -1) + (y -1) * w];
	const uchar i01 = in_gray[(x   ) + (y -1) * w];
	const uchar i02 = in_gray[(x +1) + (y -1) * w];
	const uchar i10 = in_gray[(x -1) + (y   ) * w];
	const uchar i12 = in_gray[(x +1) + (y   ) * w];
	const uchar i20 = in_gray[(x -1) + (y +1) * w];
	const uchar i21 = in_gray[(x   ) + (y +1) * w];
	const uchar i22 = in_gray[(x +1) + (y +1) * w];

	const float sum_x =
		dot(m1, convert_float4((uchar4)(i00, i10, i20, 0))) +
		dot(m2, convert_float4((uchar4)(i02, i12, i22, 0)));

	const float sum_y = 
		dot(m1, convert_float4((uchar4)(i00, i01, i02, 0))) +
		dot(m2, convert_float4((uchar4)(i20, i21, i22, 0)));
		
	gradient_u8(out_sobel_gradient, out_sobel_direction, d, sum_x, sum_y);
}