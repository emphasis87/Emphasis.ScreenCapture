#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

constant sampler_t sampler = 
	CLK_NORMALIZED_COORDS_FALSE | 
	CLK_FILTER_NEAREST | 
	CLK_ADDRESS_CLAMP_TO_EDGE;

//constant float4 gray_mask = { 0.2989f, 0.5870f, 0.1140f, 0 };
constant float4 gray_mask = 
{ 
	0.2126f, // R
	0.7152f, // G
	0.0722f, // B
	0
};

void kernel grayscale_u8(
	read_only image2d_t in_image, 
	global uchar* out_grayscale) 
{
    const int2 gid = { get_global_id(0), get_global_id(1) };
	const int2 size = { get_image_width(in_image), get_image_height(in_image) };
	int d = gid.x + gid.y * size.x;

	// read the image values as in range [0.0, 1.0]
	float4 p = read_imagef(in_image, sampler, gid);
	float gray = dot(p, gray_mask) * 255;
	
	out_grayscale[d] = convert_uchar_sat(gray);
}
