#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

void kernel threshold_u8(
	global uchar* source,
	global uchar* target,
	uchar threshold)
{
	int x = get_global_id(0);
    const uchar value = source[x];
	if (value < threshold)
		target[x] = 0;
	else
		target[x] = 255;
}

void kernel double_threshold_u8(
	global uchar* source,
	global uchar* target,
	uchar low_threshold,
	uchar high_threshold)
{
	const int x = get_global_id(0);
	const uchar value = source[x];
	if (value < low_threshold)
		target[x] = 0;
	else if (value > high_threshold)
		target[x] = 255;
}
