using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VertexClassification
{
	Front = 1 << 0,
	Back = 1 << 1,
	OnPlane = 1 << 2,
}

public class MeshCutter
{
	private CuttableMesh inputMesh;

	private List<CuttableSubMesh> outputFrontSubMeshes;
	private List<CuttableSubMesh> outputBackSubMeshes;

	public MeshCutter()
	{

	}

	public void Cut(CuttableMesh input, Plane worldCutPlane)
	{
		this.inputMesh = input;
		this.outputFrontSubMeshes = new List<CuttableSubMesh>();
		this.outputBackSubMeshes = new List<CuttableSubMesh>();

		// Transform cut plane from world space to local space

		Plane localPlane;

		Transform space = inputMesh.GetTransform();
		if (space != null)
		{
			Vector3 localPlaneOrigin = space.InverseTransformPoint(ClosestPointOnPlane(worldCutPlane, Vector3.zero));
			Vector3 localPlaneNormal = space.InverseTransformDirection(worldCutPlane.normal);
			localPlane = new Plane(localPlaneNormal, localPlaneOrigin);
		}
		else
		{
			localPlane = worldCutPlane;
		}

		foreach (CuttableSubMesh inputSubMesh in input.GetSubMeshes())
		{
			Cut(inputSubMesh, localPlane);
		}
	}
	private static Vector3 ClosestPointOnPlane(Plane plane, Vector3 point)
	{
#if UNITY_2017_1_OR_NEWER
		return plane.ClosestPointOnPlane(point);
#else
		float dist = plane.GetDistanceToPoint(point);
		
		if (plane.GetSide(point))
		{
			return point - (plane.normal * dist);
		}
		else
		{
			return point + (plane.normal * dist);
		}
#endif
	}

	public CuttableMesh GetFrontOutput()
	{
		CuttableMesh newMesh = new CuttableMesh(inputMesh, outputFrontSubMeshes);
		return newMesh;
	}

	public CuttableMesh GetBackOutput()
	{
		CuttableMesh newMesh = new CuttableMesh(inputMesh, outputBackSubMeshes);
		return newMesh;
	}

	private void Cut(CuttableSubMesh inputSubMesh, Plane cutPlane)
	{
		bool hasNormals = inputSubMesh.HasNormals();
		bool hasColours = inputSubMesh.HasColours();
		bool hasUvs = inputSubMesh.HasUvs();
		bool hasUv1 = inputSubMesh.HasUv1();

		CuttableSubMesh frontSubMesh = new CuttableSubMesh(hasNormals, hasColours, hasUvs, hasUv1);
		CuttableSubMesh backSubMesh = new CuttableSubMesh(hasNormals, hasColours, hasUvs, hasUv1);

		for (int i = 0; i < inputSubMesh.NumVertices(); i += 3)
		{
			int i0 = i;
			int i1 = i + 1;
			int i2 = i + 2;

			Vector3 v0 = inputSubMesh.GetVertex(i0);
			Vector3 v1 = inputSubMesh.GetVertex(i1);
			Vector3 v2 = inputSubMesh.GetVertex(i2);

			VertexClassification c0 = Classify(v0, cutPlane);
			VertexClassification c1 = Classify(v1, cutPlane);
			VertexClassification c2 = Classify(v2, cutPlane);

			int numFront = 0;
			int numBehind = 0;

			CountSides(c0, ref numFront, ref numBehind);
			CountSides(c1, ref numFront, ref numBehind);
			CountSides(c2, ref numFront, ref numBehind);

			if (numFront > 0 && numBehind == 0)
			{
				// Fully in front
				KeepTriangle(i0, i1, i2, inputSubMesh, frontSubMesh);
			}
			else if (numFront == 0 && numBehind > 0)
			{
				// Fully behind
				KeepTriangle(i0, i1, i2, inputSubMesh, backSubMesh);
			}
			else if (numFront == 2 && numBehind == 1)
			{
				// Split A - generate two triangles in front, one behind

				// Figure out which vertex is behind
				if (c0 == VertexClassification.Back)
				{
					SplitA(i0, i1, i2, inputSubMesh, cutPlane, backSubMesh, frontSubMesh);
				}
				else if (c1 == VertexClassification.Back)
				{
					SplitA(i1, i2, i0, inputSubMesh, cutPlane, backSubMesh, frontSubMesh);
				}
				else // c2 == Back
				{
					SplitA(i2, i0, i1, inputSubMesh, cutPlane, backSubMesh, frontSubMesh);
				}
			}
			else if (numFront == 1 && numBehind == 2)
			{
				// Split A (flipped) - generate one triangle in front, two in behind

				// Figure out which single vertex is in front
				if (c0 == VertexClassification.Front)
				{
					SplitA(i0, i1, i2, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
				}
				else if (c1 == VertexClassification.Front)
				{
					SplitA(i1, i2, i0, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
				}
				else // c2 == front
				{
					SplitA(i2, i0, i1, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
				}
			}
			else if (numFront == 1 && numBehind == 1)
			{
				// Split B - triangle split exactly along middle vertex, split into one triangle each side
				//	vertices can be either front->plane->back (normal) or front->back->plane (flipped)

				// Find our pivot vertex
				if (c0 == VertexClassification.OnPlane)
				{
					if (c2 == VertexClassification.Front)
					{
						SplitB(i2, i0, i1, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
					}
					else
					{
						SplitBFlipped(i1, i2, i0, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
					}
				}
				else if (c1 == VertexClassification.OnPlane)
				{
					if (c0 == VertexClassification.Front)
					{
						SplitB(i0, i1, i2, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
					}
					else
					{
						SplitBFlipped(i2, i0, i1, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
					}
				}
				else // c2 == on plane
				{
					// Check previous vertex for front/back
					if (c1 == VertexClassification.Front)
					{
						SplitB(i1, i2, i0, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
					}
					else
					{
						SplitBFlipped(i0, i1, i2, inputSubMesh, cutPlane, frontSubMesh, backSubMesh);
					}
				}
			}
			else if (numFront == 0 && numBehind == 0)
			{
				// Triangle lies exactly on the plane - use normal to decide whether to keep in front or back

				Vector3 d0 = v1 - v0;
				Vector3 d1 = v2 - v0;
				Vector3 normal = Vector3.Cross(d0, d1);
				if (Vector3.Dot(normal, cutPlane.normal) > 0)
				{
					KeepTriangle(i0, i1, i2, inputSubMesh, backSubMesh); // test
				}
				else
				{
					KeepTriangle(i0, i1, i2, inputSubMesh, frontSubMesh); // test
				}
			}
		}

		outputFrontSubMeshes.Add(frontSubMesh);
		outputBackSubMeshes.Add(backSubMesh);
	}

	private VertexClassification Classify(Vector3 vertex, Plane cutPlane)
	{
		// TODO: Need to swap out and use a Plane3d for better precision here
		Vector3 tmp = new Vector3((float)vertex.x, (float)vertex.y, (float)vertex.z);
		float dist = cutPlane.GetDistanceToPoint(tmp);

		double tolerance = 0.00001f;

		if (dist > -tolerance && dist < tolerance)
			return VertexClassification.OnPlane;
		else if (dist > 0)
			return VertexClassification.Front;
		else
			return VertexClassification.Back;
	}

	private void CountSides(VertexClassification c, ref int numFront, ref int numBehind)
	{
		if (c == VertexClassification.Front)
			numFront++;
		else if (c == VertexClassification.Back)
			numBehind++;
	}

	private void KeepTriangle(int i0, int i1, int i2, CuttableSubMesh inputSubMesh, CuttableSubMesh destSubMesh)
	{
		destSubMesh.CopyVertex(i0, inputSubMesh);
		destSubMesh.CopyVertex(i1, inputSubMesh);
		destSubMesh.CopyVertex(i2, inputSubMesh);
	}

	// Single vertex on +ve side (i0), other two on -ve side
	private void SplitA(int i0, int i1, int i2, CuttableSubMesh inputSubMesh, Plane cutPlane, CuttableSubMesh frontSubMesh, CuttableSubMesh backSubMesh)
	{
		// Calculate intersection points along edges i0->i1 and i2->i0

		Vector3 v0 = inputSubMesh.GetVertex(i0);
		Vector3 v1 = inputSubMesh.GetVertex(i1);
		Vector3 v2 = inputSubMesh.GetVertex(i2);

		// Intersection A
		float weightA;
		CalcIntersection(v0, v1, cutPlane, out weightA);

		// Intersection B
		float weightB;
		CalcIntersection(v2, v0, cutPlane, out weightB);

		// First front side - single new triangle from v0-A-B

		frontSubMesh.CopyVertex(i0, inputSubMesh);
		frontSubMesh.AddInterpolatedVertex(i0, i1, weightA, inputSubMesh);
		frontSubMesh.AddInterpolatedVertex(i2, i0, weightB, inputSubMesh);

		// Back side - two triangles, A-v1-v2 and v2-B-A

		backSubMesh.AddInterpolatedVertex(i0, i1, weightA, inputSubMesh);
		backSubMesh.CopyVertex(i1, inputSubMesh);
		backSubMesh.CopyVertex(i2, inputSubMesh);

		backSubMesh.CopyVertex(i2, inputSubMesh);
		backSubMesh.AddInterpolatedVertex(i2, i0, weightB, inputSubMesh);
		backSubMesh.AddInterpolatedVertex(i0, i1, weightA, inputSubMesh);
	}

	// Triangle split with one vertex exactly on the plane in order: front->plane->back
	private void SplitB(int i0, int i1, int i2, CuttableSubMesh inputSubMesh, Plane cutPlane, CuttableSubMesh frontSubMesh, CuttableSubMesh backSubMesh)
	{
		Vector3 v0 = inputSubMesh.GetVertex(i0);
		Vector3 v2 = inputSubMesh.GetVertex(i2);

		float weightA;
		CalcIntersection(v2, v0, cutPlane, out weightA);

		// Front side - single triangle i0-i1-A

		frontSubMesh.CopyVertex(i0, inputSubMesh);
		frontSubMesh.CopyVertex(i1, inputSubMesh);
		frontSubMesh.AddInterpolatedVertex(i2, i0, weightA, inputSubMesh);

		// Back side - single triangle i1-i2-A

		backSubMesh.CopyVertex(i1, inputSubMesh);
		backSubMesh.CopyVertex(i2, inputSubMesh);
		backSubMesh.AddInterpolatedVertex(i2, i0, weightA, inputSubMesh);
	}

	// Triangle split with one vertex exactly on the plane in order: front->back->plane
	private void SplitBFlipped(int i0, int i1, int i2, CuttableSubMesh inputSubMesh, Plane cutPlane, CuttableSubMesh frontSubMesh, CuttableSubMesh backSubMesh)
	{
		Vector3 v0 = inputSubMesh.GetVertex(i0);
		Vector3 v1 = inputSubMesh.GetVertex(i1);

		float weightA;
		CalcIntersection(v0, v1, cutPlane, out weightA);

		// Front side - single triangle i0-A-i2

		frontSubMesh.CopyVertex(i0, inputSubMesh);
		frontSubMesh.AddInterpolatedVertex(i0, i1, weightA, inputSubMesh);
		frontSubMesh.CopyVertex(i2, inputSubMesh);

		// Back side - single triangle i1-i2-A

		backSubMesh.CopyVertex(i1, inputSubMesh);
		backSubMesh.CopyVertex(i2, inputSubMesh);
		backSubMesh.AddInterpolatedVertex(i0, i1, weightA, inputSubMesh);
	}

	private Vector3 CalcIntersection(Vector3 v0, Vector3 v1, Plane plane, out float weight)
	{
		// Calculate length between vertices and create ray from v0 to v1
		Vector3 deltaA = v1 - v0;
		float lengthA = deltaA.magnitude;
		Ray rayA = new Ray(v0, deltaA / lengthA);

		// Raycast against the plane to find the intersection point and distance
		float distA;
		plane.Raycast(rayA, out distA);
		Vector3 intersectionA = rayA.origin + rayA.direction * distA;

		// Calculate the normalised distance between v0 and v1 of the intersection point
		weight = distA / lengthA;

		return intersectionA;
	}
}
