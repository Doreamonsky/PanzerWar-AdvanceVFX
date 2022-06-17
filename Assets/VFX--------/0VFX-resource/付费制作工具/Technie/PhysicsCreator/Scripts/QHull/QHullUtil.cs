using System.Collections.Generic;
using UnityEngine;
using Technie.PhysicsCreator.QHull;

namespace Technie.PhysicsCreator
{
	public class QHullUtil
	{
		public static Mesh FindConvexHull(string debugName, Mesh inputMesh, bool showErrorInLog)
		{
			// Generate array of input points

			Vector3[] inputVertices = inputMesh.vertices;
			int[] inputIndices = inputMesh.triangles;

			Point3d[] inputPoints = new Point3d[inputIndices.Length];

			for (int i = 0; i < inputIndices.Length; i+=3)
			{
				Vector3 p0 = inputVertices[inputIndices[i]];
				Vector3 p1 = inputVertices[inputIndices[i + 1]];
				Vector3 p2 = inputVertices[inputIndices[i + 2]];

				inputPoints[i] = new Point3d(p0.x, p0.y, p0.z);
				inputPoints[i + 1] = new Point3d(p1.x, p1.y, p1.z);
				inputPoints[i + 2] = new Point3d(p2.x, p2.y, p2.z);
			}

			// Calculate the convex hull

			QuickHull3D qHull = new QuickHull3D();
			try
			{
				qHull.build(inputPoints);
			}
			catch (System.Exception)
			{
				if (showErrorInLog)
					Debug.LogError("Could not generate convex hull for " + debugName);
			}

			// Get calculated hull points and indices

			Point3d[] hullPoints = qHull.getVertices();
			int[][] hullFaceIndices = qHull.getFaces();

			// Convert to dest vertices

			Vector3[] hullVertices = new Vector3[hullPoints.Length];
			for (int i = 0; i < hullVertices.Length; i++)
			{
				hullVertices[i] = new Vector3((float)hullPoints[i].x, (float)hullPoints[i].y, (float)hullPoints[i].z);
			}

			// Convert to dest indices

			List<int> destIndices = new List<int>();

			for (int i = 0; i < hullFaceIndices.Length; i++)
			{
				int faceVerts = hullFaceIndices[i].Length;
				for (int j = 1; j < faceVerts - 1; j++)
				{
					destIndices.Add(hullFaceIndices[i][0]);
					destIndices.Add(hullFaceIndices[i][j]);
					destIndices.Add(hullFaceIndices[i][j + 1]);
				}
			}

			int[] hullIndices = destIndices.ToArray();

			Mesh outMesh = new Mesh();
			outMesh.vertices = hullVertices;
			outMesh.triangles = hullIndices;
			return outMesh;
		}

		public static void FindConvexHull(string debugName, int[] selectedFaces, Vector3[] meshVertices, int[] meshIndices, out Vector3[] hullVertices, out int[] hullIndices, bool showErrorInLog)
		{
			if (selectedFaces.Length == 0)
			{
				hullVertices = new Vector3[0];
				hullIndices = new int[0];
				return;
			}

			// Generate array of input points

			int totalFaces = selectedFaces.Length;
			Point3d[] inputPoints = new Point3d[totalFaces * 3];

			for (int i = 0; i < selectedFaces.Length; i++)
			{
				int faceIndex = selectedFaces[i];

				Vector3 p0 = meshVertices[meshIndices[faceIndex * 3]];
				Vector3 p1 = meshVertices[meshIndices[faceIndex * 3 + 1]];
				Vector3 p2 = meshVertices[meshIndices[faceIndex * 3 + 2]];

				inputPoints[i * 3] = new Point3d(p0.x, p0.y, p0.z);
				inputPoints[i * 3 + 1] = new Point3d(p1.x, p1.y, p1.z);
				inputPoints[i * 3 + 2] = new Point3d(p2.x, p2.y, p2.z);
			}

			// Calculate the convex hull

			QuickHull3D qHull = new QuickHull3D();
			try
			{
				qHull.build(inputPoints);
			}
			catch (System.Exception)
			{
				if (showErrorInLog)
					Debug.LogError("Could not generate convex hull for " + debugName);
			}

			// Get calculated hull points and indices

			Point3d[] hullPoints = qHull.getVertices();
			int[][] hullFaceIndices = qHull.getFaces();

			// Convert to dest vertices

			hullVertices = new Vector3[hullPoints.Length];
			for (int i = 0; i < hullVertices.Length; i++)
			{
				hullVertices[i] = new Vector3((float)hullPoints[i].x, (float)hullPoints[i].y, (float)hullPoints[i].z);
			}

			// Convert to dest indices

			List<int> destIndices = new List<int>();

			for (int i = 0; i < hullFaceIndices.Length; i++)
			{
				int faceVerts = hullFaceIndices[i].Length;
				for (int j = 1; j < faceVerts - 1; j++)
				{
					destIndices.Add(hullFaceIndices[i][0]);
					destIndices.Add(hullFaceIndices[i][j]);
					destIndices.Add(hullFaceIndices[i][j + 1]);
				}
			}

			hullIndices = destIndices.ToArray();
		}
	}
}