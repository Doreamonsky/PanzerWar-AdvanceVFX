using UnityEngine;

namespace Technie.PhysicsCreator
{
	[System.Serializable]
	public class VhacdParameters
	{
		public VhacdParameters()
		{
			resolution = 100000;
			concavity = 0.001f;
			planeDownsampling = 4;
			convexhullDownsampling = 4;
			alpha = 0.05f;
			beta = 0.05f;
			pca = 0;
			mode = 0;
			maxNumVerticesPerCH = 64;
			minVolumePerCH = 0.0001f;
			convexhullApproximation = 1;
			oclAcceleration = 0;
			maxConvexHulls = 1024;
			projectHullVertices = true;
		}

		[Tooltip("maximum concavity")]
		[Range(0.0f, 1.0f)]
		public float concavity;

		[Tooltip("controls the bias toward clipping along symmetry planes")]
		[Range(0, 1)]
		public float alpha;

		[Tooltip("controls the bias toward clipping along revolution axes")]
		[Range(0, 1)]
		public float beta;

		[Tooltip("controls the adaptive sampling of the generated convex-hulls")]
		[Range(0, 0.01f)]
		public float minVolumePerCH;

		[Tooltip("maximum number of voxels generated during the voxelization stage")]
		[Range(10000, 64000000)]
		public uint resolution;

		[Tooltip("controls the maximum number of triangles per convex-hull")]
		[Range(4, 1024)]
		public uint maxNumVerticesPerCH;

		[Tooltip("controls the granularity of the search for the \"best\" clipping plane")]
		[Range(1, 16)]
		public uint planeDownsampling;

		[Tooltip("controls the precision of the convex-hull generation process during the clipping plane selection stage")]
		[Range(1, 16)]
		public uint convexhullDownsampling;

		[Tooltip("enable/disable normalizing the mesh before applying the convex decomposition")]
		[Range(0, 1)]
		public uint pca;

		[Tooltip("0: voxel-based (recommended), 1: tetrahedron-based")]
		[Range(0, 1)]
		public uint mode;

		[Range(0, 1)]
		public uint convexhullApproximation;

		[Tooltip("Enable OpenCL acceleration")]
		[Range(0, 1)]
		public uint oclAcceleration;

		public uint maxConvexHulls;

		[Tooltip("This will project the output convex hull vertices onto the original source mesh to increase the floating point accuracy of the results")]
		public bool projectHullVertices;
	}
}
