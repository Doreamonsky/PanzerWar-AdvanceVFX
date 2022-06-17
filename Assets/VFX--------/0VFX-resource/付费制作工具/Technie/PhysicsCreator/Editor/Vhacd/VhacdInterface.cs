using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Technie.PhysicsCreator
{
	[System.Serializable]
	public unsafe struct RawVhacdParameters
	{
		public RawVhacdParameters(VhacdParameters src)
		{
			this.m_concavity = src.concavity;
			this.m_alpha = src.alpha;
			this.m_beta = src.beta;
			this.m_minVolumePerCH = src.minVolumePerCH;
			this.m_callback = null;
			this.m_logger = null;
			this.m_resolution = src.resolution;
			this.m_maxNumVerticesPerCH = src.maxNumVerticesPerCH;
			this.m_resolution = src.resolution;
			this.m_planeDownsampling = src.planeDownsampling;
			this.m_convexhullDownsampling = src.convexhullDownsampling;
			this.m_pca = src.pca;
			this.m_mode = src.mode;
			this.m_convexhullApproximation = src.convexhullApproximation;
			this.m_oclAcceleration = src.oclAcceleration;
			this.m_maxConvexHulls = src.maxConvexHulls;
			this.m_projectHullVertices = src.projectHullVertices;
		}

		[Tooltip("maximum concavity")]
		[Range(0, 1)]
		public double m_concavity;

		[Tooltip("controls the bias toward clipping along symmetry planes")]
		[Range(0, 1)]
		public double m_alpha;

		[Tooltip("controls the bias toward clipping along revolution axes")]
		[Range(0, 1)]
		public double m_beta;

		[Tooltip("controls the adaptive sampling of the generated convex-hulls")]
		[Range(0, 0.01f)]
		public double m_minVolumePerCH;

		public void* m_callback;
		public void* m_logger;

		[Tooltip("maximum number of voxels generated during the voxelization stage")]
		[Range(10000, 64000000)]
		public uint m_resolution;

		[Tooltip("controls the maximum number of triangles per convex-hull")]
		[Range(4, 1024)]
		public uint m_maxNumVerticesPerCH;

		[Tooltip("controls the granularity of the search for the \"best\" clipping plane")]
		[Range(1, 16)]
		public uint m_planeDownsampling;

		[Tooltip("controls the precision of the convex-hull generation process during the clipping plane selection stage")]
		[Range(1, 16)]
		public uint m_convexhullDownsampling;

		[Tooltip("enable/disable normalizing the mesh before applying the convex decomposition")]
		[Range(0, 1)]
		public uint m_pca;

		[Tooltip("0: voxel-based (recommended), 1: tetrahedron-based")]
		[Range(0, 1)]
		public uint m_mode;

		[Range(0, 1)]
		public uint m_convexhullApproximation;

		[Range(0, 1)]
		public uint m_oclAcceleration;
		
		public uint m_maxConvexHulls;

		[Tooltip("This will project the output convex hull vertices onto the original source mesh to increase the floating point accuracy of the results")]
		public bool m_projectHullVertices;
	}

	public unsafe struct ConvexHull
	{
		public double* m_points;
		public uint* m_triangles;
		public uint m_nPoints;
		public uint m_nTriangles;
		public double m_volume;
		public fixed double m_center[3];
	}

	public class VhacdInterface
	{
		[DllImport("libvhacd")]
		public static extern unsafe void* CreateVHACD();

		[DllImport("libvhacd")]
		public static extern unsafe void DestroyVHACD(void* pVHACD);

		[DllImport("libvhacd")]
		public static extern unsafe bool ComputeFloat(
			void* pVHACD,
			float* points,
			uint countPoints,
			uint* triangles,
			uint countTriangles,
			ref RawVhacdParameters parameters);

		[DllImport("libvhacd")]
		public static extern unsafe bool ComputeDouble(
			void* pVHACD,
			double* points,
			uint countPoints,
			uint* triangles,
			uint countTriangles,
			RawVhacdParameters* parameters);

		[DllImport("libvhacd")]
		public static extern unsafe uint GetNConvexHulls(void* pVHACD);

		[DllImport("libvhacd")]
		public static extern unsafe void GetConvexHull(
			void* pVHACD,
			uint index,
			ConvexHull* ch);
	}
}