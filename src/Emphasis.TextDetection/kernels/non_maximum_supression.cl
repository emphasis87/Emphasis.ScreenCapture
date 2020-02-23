#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

/*
constant short2 W  = { -1,  0 };
constant short2 NW = { -1, -1 };
constant short2 N  = {  0, -1 };
constant short2 NE = {  1, -1 };
constant short2 E  = {  1,  0 };
constant short2 SE = {  1,  1 };
constant short2 S  = {  0,  1 };
constant short2 SW = {  1,  1 };
*/

constant short4 edge_neighbours[] = 
{   
	// x0, y0, x1, y1
	{ -1,  0,  1,  0 }, // W/E
	{ -1, -1,  1,  1 }, // SW/NE
    {  0, -1,  0,  1 }, // S/N
    {  1, -1, -1,  1 }, // SE/NW
	{  1, -1, -1,  1 }, // E/W
	{  1, -1, -1,  1 }, // NE/SW
	{  1, -1, -1,  1 }, // N/S
	{  1, -1, -1,  1 }, // NW/SE
	{  1, -1, -1,  1 }, // W/E
	{  1, -1, -1,  1 }, // W/E
};

void kernel non_maximum_suppression_u8(
	global uchar* in_gradient,
	global uchar* in_angle,
	global uchar* out_nms,
	int min_gradient)
{
	int x = get_global_id(0);
    int y = get_global_id(1);
	int w = get_global_size(0);
    int h = get_global_size(1);
	int d = x + y * w;

	uchar gradient = in_gradient[d];

	if (gradient < min_gradient){
		out_nms[d] = 0;
		return;
	}

	uchar angle = in_angle[d];
	short4 n = edge_neighbours[angle];
	
	short2 n1 = n.s01;
	uchar g1 = in_gradient[x + n1.x + (y + n1.y) * w];

	short2 n2 = n.s23;
	uchar g2 = in_gradient[x + n2.x + (y + n2.y) * w];

	// Suppress gradient if neighbours are larger
	if (g1 > gradient || g2 > gradient) 
		out_nms[d] = 0;
	else
		out_nms[d] = gradient;
}
