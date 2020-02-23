//#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

#define NUM_BANKS 16
#define LOG_NUM_BANKS 4
#define CONFLICT_FREE_OFFSET(n) \
    ((n) >> NUM_BANKS + (n) >> (2 * LOG_NUM_BANKS))

inline int bank_offset(int n){ 
	return ((n) >> NUM_BANKS + (n) >> (2 * LOG_NUM_BANKS));
}

void kernel prefix_scan(
	global int* in,
	global int* out,
	local float* ldata,
	int n) 
{
	int x = get_global_id(0);
	int w = get_global_size(0);
    
	int offset = 1;

	int ai = 2*x;
	int bi = x + (n/2);

	//ldata[ai] = in[ai];
	//ldata[bi] = in[bi];
	
	for (int d = n >> 1; d > 0; d >>= 1)
	{
		barrier(CLK_LOCAL_MEM_FENCE);
		
		 

		
		//if (x < d)
		//{
			
			int ci = offset * (2*x + 1) -1;
			int di = offset * (2*x + 2) -1;
			
			ldata[ci] = ci;
		//}

		offset *= 2;
		break;
	}

	// build sum in place up the tree
	// d = n/2, n/4, n/8, ..., 1
	// o = 1, 2, 4, 8, ..., n/2
	
	

	/*
	for (int d = n>>1; d > 0; d >>= 1)
	{ 
		work_group_barrier(CLK_LOCAL_MEM_FENCE);

		// Only first n/2, n/4, ..., 1 global threads
		if (x < d)
		{
			int ai = offset * (2*x + 1) -1;
			int bi = offset * (2*x + 2) -1;

			ldata[bi] += ldata[ai];
		}
		offset *= 2;
	}
	*/
	
	/*
	if (thid==0) { ldata[n - 1 + CONFLICT_FREE_OFFSET(n - 1)] = 0;} // clear the last element

	for (int d = 1; d < n; d *= 2) // traverse down tree & build scan
	{
		offset >>= 1;
		work_group_barrier(CLK_LOCAL_MEM_FENCE);
		if (thid < d)                     
		{
			int ai = offset*(2*thid+1)-1;
			int bi = offset*(2*thid+2)-1;
			ai += CONFLICT_FREE_OFFSET(ai);
			bi += CONFLICT_FREE_OFFSET(bi);
       
			float t = ldata[ai];
			ldata[ai] = ldata[bi];
			ldata[bi] += t; 
		}
	}
	work_group_barrier(CLK_LOCAL_MEM_FENCE);
	*/

	out[ai] = ldata[ai];
	out[bi] = ldata[bi];

	//out[ai] = ai;
	//out[bi] = bi;

}
