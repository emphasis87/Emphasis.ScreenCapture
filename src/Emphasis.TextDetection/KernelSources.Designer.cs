﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Emphasis.TextDetection {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class KernelSources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal KernelSources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Emphasis.TextDetection.KernelSources", typeof(KernelSources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
        ///
        ///constant sampler_t sampler = 
        ///	CLK_NORMALIZED_COORDS_FALSE | 
        ///	CLK_FILTER_NEAREST | 
        ///	CLK_ADDRESS_CLAMP_TO_EDGE;
        ///
        ///constant int sobx[3][3] = 
        ///{
        ///	{-1, 0, 1},
        ///	{-2, 0, 2},
        ///	{-1, 0, 1},
        ///};
        ///
        ///constant int soby[3][3] = 
        ///{
        ///	{-1,-2,-1},
        ///	{ 0, 0, 0},
        ///	{ 1, 2, 1},
        ///};
        ///
        ///constant int scharrx[3][3] = 
        ///{
        ///	{-3,  0, 3 },
        ///	{-10, 0, 10},
        ///	{-3,  0, 3 },
        ///};
        ///
        ///constant int scharry[3][3] = 
        ///{
        ///	{-3,  -10, -3 },
        ///	{ 0,    0,  0 },
        ///	{ 3,    [rest of string was truncated]&quot;;.
        /// </summary>
        public static string canny {
            get {
                return ResourceManager.GetString("canny", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
        ///
        ///constant sampler_t sampler = 
        ///	CLK_NORMALIZED_COORDS_FALSE | 
        ///	CLK_FILTER_NEAREST | 
        ///	CLK_ADDRESS_CLAMP_TO_EDGE;
        ///
        /////constant float4 gray_mask = { 0.2989f, 0.5870f, 0.1140f, 0 };
        ///constant float4 gray_mask = 
        ///{ 
        ///	0.2126f, // R
        ///	0.7152f, // G
        ///	0.0722f, // B
        ///	0
        ///};
        ///
        ///void kernel grayscale_u8(
        ///	read_only image2d_t in_image, 
        ///	global uchar* out_grayscale) 
        ///{
        ///    const int2 gid = { get_global_id(0), get_global_id(1) };
        ///	const int2  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string grayscale {
            get {
                return ResourceManager.GetString("grayscale", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
        ///
        ///void kernel hysteresis(
        ///	global uchar* in_nms,
        ///	global uchar* out_hysteresis) 
        ///{
        ///	int x = get_global_id(0);
        ///    int y = get_global_id(1);
        ///	int w = get_global_size(0);
        ///    int h = get_global_size(1);
        ///	int d = x + y * w;
        ///
        ///}
        ///.
        /// </summary>
        public static string hysteresis {
            get {
                return ResourceManager.GetString("hysteresis", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
        ///
        ////*
        ///constant short2 W  = { -1,  0 };
        ///constant short2 NW = { -1, -1 };
        ///constant short2 N  = {  0, -1 };
        ///constant short2 NE = {  1, -1 };
        ///constant short2 E  = {  1,  0 };
        ///constant short2 SE = {  1,  1 };
        ///constant short2 S  = {  0,  1 };
        ///constant short2 SW = {  1,  1 };
        ///*/
        ///
        ///constant short4 edge_neighbours[] = 
        ///{   
        ///	// x0, y0, x1, y1
        ///	{ -1,  0,  1,  0 }, // W/E
        ///	{ -1, -1,  1,  1 }, // SW/NE
        ///    {  0, -1,  0,  1 }, // S/N
        ///    {   [rest of string was truncated]&quot;;.
        /// </summary>
        public static string non_maximum_supression {
            get {
                return ResourceManager.GetString("non_maximum_supression", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to //#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
        ///
        ///#define NUM_BANKS 16
        ///#define LOG_NUM_BANKS 4
        ///#define CONFLICT_FREE_OFFSET(n) \
        ///    ((n) &gt;&gt; NUM_BANKS + (n) &gt;&gt; (2 * LOG_NUM_BANKS))
        ///
        ///inline int bank_offset(int n){ 
        ///	return ((n) &gt;&gt; NUM_BANKS + (n) &gt;&gt; (2 * LOG_NUM_BANKS));
        ///}
        ///
        ///void kernel prefix_scan(
        ///	global int* in,
        ///	global int* out,
        ///	local float* ldata,
        ///	int n) 
        ///{
        ///	int x = get_global_id(0);
        ///	int w = get_global_size(0);
        ///    
        ///	int offset = 1;
        ///
        ///	int ai = 2*x;
        ///	int bi = x  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string prefix_scan {
            get {
                return ResourceManager.GetString("prefix_scan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
        ///
        ///void kernel sobel_u8(
        ///	global uchar* in_gray,
        ///	global uchar* out_sobel_x,
        ///	global uchar* out_sobel_y,
        ///	global uchar* out_sobel_gradient,
        ///	global uchar* out_sobel_angle) 
        ///{
        ///	int x = get_global_id(0);
        ///    int y = get_global_id(1);
        ///	int w = get_global_size(0);
        ///    int h = get_global_size(1);
        ///
        ///	if (x == 0 || x == w-1 || y == 0 || y == h-1)
        ///		return;
        ///
        ///	int d = y * w + x;
        ///
        ///	uchar i00 = in_gray[(x -1) + (y -1) * w];
        ///	uchar i01 = i [rest of string was truncated]&quot;;.
        /// </summary>
        public static string sobel {
            get {
                return ResourceManager.GetString("sobel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to #pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
        ///
        ///void kernel threshold_u8(
        ///	global uchar* source,
        ///	global uchar* target,
        ///	int threshold,
        ///	uchar lower_than_value,
        ///	uchar higher_than_value)
        ///{
        ///	int x = get_global_id(0);
        ///    target[x] = source[x] &lt; threshold ? lower_than_value : higher_than_value;
        ///}
        ///.
        /// </summary>
        public static string threshold {
            get {
                return ResourceManager.GetString("threshold", resourceCulture);
            }
        }
    }
}
