#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

void kernel hysteresis(
	global uchar* in_nms,
	global uchar* out_hysteresis) 
{
	int x = get_global_id(0);
    int y = get_global_id(1);
	int w = get_global_size(0);
    int h = get_global_size(1);
	int d = x + y * w;

}
