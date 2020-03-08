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

constant short4 edge_perpendicular_neighbours[] = 
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
};

void kernel stroke_width_transform_u8(
	global uchar* in_canny,
	global uchar* in_direction,
	global uchar* out_stroke_width,
	int max_stroke_width)
{
	int x = get_global_id(0);
    int y = get_global_id(1);
	int w = get_global_size(0);
    int h = get_global_size(1);
	int d = x + y * w;

	uchar direction = in_direction[d];
	short4 n = edge_perpendicular_neighbours[direction];

	// Find the neighbouring positions in the perpendicular direction to the edge direction
	short2 n1 = n.s01;
	short2 n2 = n.s23;
	int d1 = x + n1.x + (y + n1.y) * w;
	int d2 = x + n2.x + (y + n2.y) * w

	uchar g1 = in_canny[d1];
	uchar g2 = in_canny[d2];
}
