#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

void kernel threshold_u8(
	global uchar* source,
	global uchar* target,
	uchar threshold,
	uchar lower_than_value,
	uchar higher_than_value)
{
	int x = get_global_id(0);
    target[x] = source[x] < threshold ? lower_than_value : higher_than_value;
}
