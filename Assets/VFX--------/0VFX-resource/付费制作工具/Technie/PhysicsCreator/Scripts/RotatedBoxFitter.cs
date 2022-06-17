using System.Collections.Generic;
using UnityEngine;

namespace Technie.PhysicsCreator
{
	public enum BoxFitMethod
	{
		AxisAligned,
		MinimumVolume,
		AlignFaces
	}

	public class ConstructionPlane
	{
		public Vector3 center;
		public Vector3 normal;
		public Vector3 tangent;

		public Quaternion rotation;

		public Matrix4x4 planeToWorld;
		public Matrix4x4 worldToPlane;

		public ConstructionPlane(Vector3 c, Vector3 n, Vector3 t)
		{
			this.center = c;
			this.normal = n;
			this.tangent = t;

			Init();
		}

		public ConstructionPlane(ConstructionPlane basePlane, float angle)
		{
			Quaternion q = Quaternion.AngleAxis(angle, basePlane.normal);
			Vector3 rotatedTangent = q * basePlane.tangent;

			this.center = basePlane.center;
			this.normal = basePlane.normal;
			this.tangent = rotatedTangent;

			Init();
		}

		private void Init()
		{
			if (normal.magnitude < 0.01f)
				Debug.LogError("!");

			this.rotation = Quaternion.LookRotation(normal, tangent);

			this.planeToWorld = Matrix4x4.TRS(center, rotation, Vector3.one);
			this.worldToPlane = planeToWorld.inverse;
		}
	}

	public class RotatedBox
	{
		public ConstructionPlane plane;

		public Vector3 localCenter;
		public Vector3 center;
		public Vector3 size;
		public float volume;

		public float VolumeCm3
		{
			get { return volume * 1000000.0f; }
		}

		public RotatedBox(ConstructionPlane p, Vector3 localCenter, Vector3 c, Vector3 s)
		{
			this.plane = p;
			this.localCenter = localCenter;
			this.center = c;
			this.size = s;
			this.volume = size.x * size.y * size.z;
		}
	}

	// Sorts rotated boxes so that the tightest (ie. smallest volume) comes first in the list
	public class VolumeSorter : IComparer<RotatedBox>
	{
		public int Compare(RotatedBox lhs, RotatedBox rhs)
		{
			if (Mathf.Approximately(lhs.volume, rhs.volume))
			{
				return 0;
			}
			else if (lhs.volume > rhs.volume)
			{
				return 1;
			}
			else
			{
				return -1;
			}
		}
	}

	public class RotatedBoxFitter
	{
		public RotatedBoxFitter()
		{

		}

		public void Fit(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			// Find find the convex hull - the tightest fitting box will always contain the hull, and this lets us simplify the input data

			Vector3[] hullVertices;
			int[] hullIndices;
			QHullUtil.FindConvexHull(hull.name, hull.selectedFaces.ToArray(), meshVertices, meshIndices, out hullVertices, out hullIndices, false);

			// If we can't generate a convex hull (maybe we have a single quad input) then just extract the selected triangles and use that as input
			if (hullVertices == null || hullVertices.Length == 0)
			{
				FaceAlignmentBoxFitter.FindTriangles(hull, meshVertices, meshIndices, out hullVertices, out hullIndices);
			}

			List<ConstructionPlane> planes = new List<ConstructionPlane>();

			// Quality / performance settings

			int initialNumVariants = 64;
			int refineNumVariants = 128;
			float refineAngleRange = (360.0f / initialNumVariants);

			// Generate a set of construction planes for each face in the convex hull

			for (int i = 0; i < hullIndices.Length; i += 3)
			{
				int i0 = hullIndices[i];
				int i1 = hullIndices[i + 1];
				int i2 = hullIndices[i + 2];

				Vector3 v0 = hullVertices[i0];
				Vector3 v1 = hullVertices[i1];
				Vector3 v2 = hullVertices[i2];

				// Calculate the base plane for this face

				Vector3 center = (v0 + v1 + v2) / 3.0f;
				Vector3 normal = Vector3.Cross((v1 - v0).normalized, (v2 - v0).normalized);
				Vector3 tmp = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
				Vector3 tangent = Vector3.Cross(normal, tmp);

				// Degenerate triangles create zero-length normals - skip these
				if (normal.magnitude > 0.0001f)
				{
					ConstructionPlane basePlane = new ConstructionPlane(center, normal, tangent);

					// Generate a series of rotated variants from the base plane
					for (int a = 0; a < initialNumVariants; a++)
					{
						float angle = ((float)a / (float)(initialNumVariants - 1)) * 360.0f;

						ConstructionPlane rotatedPlane = new ConstructionPlane(basePlane, angle);
						planes.Add(rotatedPlane);
					}
				}
			}
			
			// Find the tightest boxes for the initial planes and get the best initial box from them (based on smallest volume)
			// TODO: If we're only taking the single best we don't need to sort here but can replace with a linear scan
			List<RotatedBox> initialBoxes = FindTightestBoxes(planes, hullVertices);
			if (initialBoxes.Count > 0)
			{
				initialBoxes.Sort(new VolumeSorter());
				RotatedBox initialBest = initialBoxes[0];

				// Create a another set of planes based around the best box but with finer rotations
				List<ConstructionPlane> refiniedPlanes = new List<ConstructionPlane>();
				GeneratePlaneVariants(initialBoxes[0].plane, refineNumVariants, refineAngleRange, refiniedPlanes);

				// Try and find even tighter boxes using the refined planes from our initial guess
				// TODO: If we're only taking the single best we don't need to sort here but can replace with a linear scan
				List<RotatedBox> refinedBoxes = FindTightestBoxes(refiniedPlanes, hullVertices);
				refinedBoxes.Sort(new VolumeSorter());
				RotatedBox refinedBest = refinedBoxes[0];

				// Take the smallest and apply it's data to the hull
				ApplyToHull(refinedBest, hull);

				//	Debug.Log("Initial best volume: " + initialBest.VolumeCm3.ToString("0.00") + " cm3");
				//	Debug.Log("Refinied best volume: " + refinedBest.VolumeCm3.ToString("0.00") + " cm3");
			}
			else
			{
				Debug.LogError("Couldn't fit box rotation to hull for " + hull.name);
			}
		}

		private static void GeneratePlaneVariants(ConstructionPlane basePlane, int numVariants, float angleRange, List<ConstructionPlane> variantPlanes)
		{
			variantPlanes.Add(basePlane);

			for (int i = 0; i < numVariants; i++)
			{
				float alpha = (float)i / (float)(numVariants - 1);
				float angle = Mathf.Lerp(-angleRange, angleRange, alpha);

				variantPlanes.Add(new ConstructionPlane(basePlane, angle));
			}
		}

		public static void ApplyToHull(RotatedBox computedBox, Hull targetHull)
		{
			targetHull.collisionBox.center = computedBox.localCenter;

			targetHull.collisionBox.size = computedBox.size;

			targetHull.boxPosition = computedBox.plane.center;
			targetHull.boxRotation = computedBox.plane.rotation;
		}

		private static List<RotatedBox> FindTightestBoxes(List<ConstructionPlane> planes, Vector3[] inputVertices)
		{
			if (inputVertices == null || inputVertices.Length == 0)
				return new List<RotatedBox>();

			List<RotatedBox> boxes = new List<RotatedBox>();

			foreach (ConstructionPlane plane in planes)
			{
				RotatedBox box = FindTightestBox(plane, inputVertices);
				boxes.Add(box);
			}

			return boxes;
		}

		public static RotatedBox FindTightestBox(ConstructionPlane plane, Vector3[] inputVertices)
		{
			if (inputVertices == null || inputVertices.Length == 0)
				return null;

			Vector3 min, max;
			min = max = plane.worldToPlane.MultiplyPoint(inputVertices[0]);

			foreach (Vector3 worldPos in inputVertices)
			{
				Vector3 localPos = plane.worldToPlane.MultiplyPoint(worldPos);

				min = Vector3.Min(localPos, min);
				max = Vector3.Max(localPos, max);
			}
			
			Vector3 localCenter = Vector3.Lerp(min, max, 0.5f);
			Vector3 center = plane.planeToWorld.MultiplyPoint(localCenter);
			Vector3 size = max - min;

			RotatedBox box = new RotatedBox(plane, localCenter, center, size);
			return box;
		}
	}
}
