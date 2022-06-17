using System.Collections.Generic;
using UnityEngine;

namespace Technie.PhysicsCreator
{
	public class Pose
	{
		public Vector3 forward;
		public Vector3 up;
		public Vector3 right;
	}

	public class Triangle
	{
		public Vector3 normal;
		public float area;
		public Vector3 center;

		public Triangle(Vector3 p0, Vector3 p1, Vector3 p2)
		{
			Vector3 e0 = (p1 - p0);
			Vector3 e1 = (p2 - p0);

			Vector3 cross = Vector3.Cross(e0, e1);

			this.area = cross.magnitude * 0.5f;
			this.normal = cross.normalized;

			this.center = (p0 + p1 + p2) / 3.0f;
		}
	}
	
	public class TriangleBucket
	{
		public float Area { get { return totalArea; } }

		private List<Triangle> triangles;
		private Vector3 averagedNormal;
		private Vector3 averagedCenter;
		private float totalArea;

		public TriangleBucket(Triangle initialTriangle)
		{
			this.triangles = new List<Triangle>();
			triangles.Add(initialTriangle);
			CalculateNormal();
			CalcTotalArea();
		}

		public void Add(Triangle t)
		{
			this.triangles.Add(t);
			CalculateNormal();
			CalcTotalArea();
		}

		public void Add(TriangleBucket otherBucket)
		{
			foreach (Triangle t in otherBucket.triangles)
			{
				this.triangles.Add(t);
			}
			CalculateNormal();
			CalcTotalArea();
		}

		private void CalculateNormal()
		{
			averagedNormal = Vector3.zero;

			foreach (Triangle t in triangles)
			{
				averagedNormal += (t.normal * t.area);
			}

			averagedNormal.Normalize();
		}

		public Vector3 GetAverageNormal()
		{
			return averagedNormal;
		}

		public Vector3 GetAverageCenter()
		{
			return triangles[0].center;
		}

		private void CalcTotalArea()
		{
			totalArea = 0.0f;

			foreach (Triangle t in triangles)
			{
				totalArea += t.area;
			}
		}
	}
	
	public class TriangleAreaSorter : IComparer<Triangle>
	{
		public int Compare(Triangle lhs, Triangle rhs)
		{
			if (lhs.area < rhs.area)
				return 1;
			else if (lhs.area > rhs.area)
				return -1;
			else
				return 0;
		}
	}

	public class TriangleBucketSorter : IComparer<TriangleBucket>
	{
		public int Compare(TriangleBucket lhs, TriangleBucket rhs)
		{
			if (lhs.Area < rhs.Area)
				return 1;
			else if (lhs.Area > rhs.Area)
				return -1;
			else
				return 0;
		}
	}

	public class FaceAlignmentBoxFitter
	{
		public FaceAlignmentBoxFitter()
		{
			
		}
		
		public void Fit(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			if (meshIndices.Length < 3)
				return;
			
			// First extract the triangles from the raw mesh data
			List<Triangle> triangles = FindTriangles(meshVertices, meshIndices, hull.selectedFaces);
			triangles.Sort( new TriangleAreaSorter() ); // Sort by area, largest first
			
			// Filter the triangles into buckets, based on their normal
			List<TriangleBucket> buckets = new List<TriangleBucket>();
			foreach (Triangle t in triangles)
			{
				TriangleBucket bucket = FindBestBucket(t, 30.0f, buckets);
				if (bucket != null)
				{
					// Have an existing bucket that's close enough that we can add this triangle to it
					bucket.Add(t);
				}
				else
				{
					// Make a new bucket for this triangle
					bucket = new TriangleBucket(t);
					buckets.Add(bucket);
				}
			}
			
			// We want at maximum 3 buckets (one for each axis of the box), so keep merging similar boxes until we've reduced it to three
			while (buckets.Count > 3)
			{
				MergeClosestBuckets(buckets);
			}

			buckets.Sort( new TriangleBucketSorter() ); // largest area first

			Vector3[] selectedVertices = GetSelectedVertices(hull, meshVertices, meshIndices);
			
			// Use the first two buckets to create the construction plane, and fit the tightest box to it
			ConstructionPlane basePlane = CreateConstructionPlane(buckets[0], buckets.Count > 1 ? buckets[1] : null, buckets.Count > 2 ? buckets[2] : null);
			RotatedBox initialBox = RotatedBoxFitter.FindTightestBox(basePlane, selectedVertices);
			RotatedBoxFitter.ApplyToHull(initialBox, hull);
		}
		
		public static List<Triangle> FindTriangles(Vector3[] meshVertices, int[] meshIndices, List<int> selectedFaces)
		{
			List<Triangle> result = new List<Triangle>();
			
			foreach (int i in selectedFaces)
			{
				int i0 = meshIndices[i * 3];
				int i1 = meshIndices[i * 3 + 1];
				int i2 = meshIndices[i * 3 + 2];

				Vector3 p0 = meshVertices[i0];
				Vector3 p1 = meshVertices[i1];
				Vector3 p2 = meshVertices[i2];

				Triangle t = new Triangle(p0, p1, p2);
				result.Add(t);
			}
			
			return result;
		}

		public static void FindTriangles(Hull hull, Vector3[] meshVertices, int[] meshIndices, out Vector3[] hullVertices, out int[] hullIndices)
		{
			List<Vector3> outVertices = new List<Vector3>();
			
			foreach (int i in hull.selectedFaces)
			{
				int i0 = meshIndices[i * 3];
				int i1 = meshIndices[i * 3 + 1];
				int i2 = meshIndices[i * 3 + 2];

				Vector3 p0 = meshVertices[i0];
				Vector3 p1 = meshVertices[i1];
				Vector3 p2 = meshVertices[i2];

				outVertices.Add(p0);
				outVertices.Add(p1);
				outVertices.Add(p2);
			}

			hullVertices = outVertices.ToArray();

			hullIndices = new int[hullVertices.Length];
			for (int i=0; i<hullIndices.Length; i++)
			{
				hullIndices[i] = i;
			}
		}

		public static Vector3[] GetSelectedVertices(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			Dictionary<int, int> selectedIndices = new Dictionary<int, int>();
			foreach (int i in hull.selectedFaces)
			{
				int i0 = meshIndices[i * 3];
				int i1 = meshIndices[i * 3 + 1];
				int i2 = meshIndices[i * 3 + 2];

				selectedIndices[i0] = i0;
				selectedIndices[i1] = i1;
				selectedIndices[i2] = i2;
			}
			
			List<Vector3> selectedVertices = new List<Vector3>();
			foreach (int i in selectedIndices.Keys)
			{
				selectedVertices.Add(meshVertices[i]);
			}

			return selectedVertices.ToArray();
		}
		
		private TriangleBucket FindBestBucket(Triangle tri, float thresholdAngleDeg, List<TriangleBucket> buckets)
		{
			TriangleBucket bestBucket = null;
			float bestError = float.PositiveInfinity;

			foreach (TriangleBucket bucket in buckets)
			{
				float error = Vector3.Angle(tri.normal, bucket.GetAverageNormal());
				if (error < thresholdAngleDeg && error < bestError)
				{
					bestError = error;
					bestBucket = bucket;
				}
				else
				{
					float flippedError = Vector3.Angle(tri.normal * -1.0f, bucket.GetAverageNormal());
					if (flippedError < thresholdAngleDeg && flippedError < bestError)
					{
						tri.normal *= -1.0f;
						bestError = flippedError;
						bestBucket = bucket;
					}
				}
			}

			return bestBucket;
		}

		private void MergeClosestBuckets(List<TriangleBucket> buckets)
		{
			TriangleBucket best0 = null;
			TriangleBucket best1 = null;
			float bestError = float.PositiveInfinity;

			for (int i = 0; i < buckets.Count; i++)
			{
				for (int j = i + 1; j < buckets.Count; j++)
				{
					TriangleBucket b0 = buckets[i];
					TriangleBucket b1 = buckets[j];

					float error = Vector3.Angle(b0.GetAverageNormal(), b1.GetAverageNormal());
					if (error < bestError)
					{
						bestError = error;
						best0 = b0;
						best1 = b1;
					}
				}
			}

			if (best0 != null && best1 != null)
			{
				// Merge b1 into b0
				buckets.Remove(best1);
				best0.Add(best1);
			}
		}
		
		private ConstructionPlane CreateConstructionPlane(TriangleBucket primaryBucket, TriangleBucket secondaryBucket, TriangleBucket tertiaryBucket)
		{
	//		if (primaryBucket != null && secondaryBucket != null && tertiaryBucket != null)
			{

			}
	//		else
				if (primaryBucket != null && secondaryBucket != null)
			{
				Vector3 normal = primaryBucket.GetAverageNormal();
				Vector3 tangent = Vector3.Cross(normal, secondaryBucket.GetAverageNormal());
				Vector3 center = primaryBucket.GetAverageCenter();
				return new ConstructionPlane(center, normal, tangent);
			}
			else if (primaryBucket != null)
			{
				// Only have one bucket, so just create a plane using an arbitrary reference vector
				Vector3 normal = primaryBucket.GetAverageNormal();
				Vector3 center = primaryBucket.GetAverageCenter();
				Vector3 tangent = Vector3.Cross(normal, Vector3.Dot(normal, Vector3.up) > 0.5f ? Vector3.right : Vector3.up);
				return new ConstructionPlane(center, normal, tangent);
			}
			return null;
		}
	}
}
