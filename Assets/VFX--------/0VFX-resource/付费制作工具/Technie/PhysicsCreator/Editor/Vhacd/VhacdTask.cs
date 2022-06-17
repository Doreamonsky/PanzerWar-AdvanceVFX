using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Technie.PhysicsCreator
{
	public class VhacdTask
	{
		private class RawHull
		{
			public Vector3[] vertices;
			public int[] indices;
		}

		public Mesh[] OutputHulls { get { return this.outputHulls.ToArray(); } }

		public float DurationSecs { get { return this.durationSecs; } }

		private Thread thread;

		private bool isFinished;

		private Vector3[] inputVertices;
		private int[] inputIndices;

		private VhacdParameters parameters;

		private List<RawHull> rawHulls = new List<RawHull>();
		private List<Mesh> outputHulls = new List<Mesh>();

		private DateTime startTime;
		private float durationSecs;

		public VhacdTask()
		{
			
		}
		
		public void Init(Mesh inputMesh, VhacdParameters parameters)
		{
			this.inputVertices = inputMesh.vertices;
			this.inputIndices = inputMesh.triangles;

			this.parameters = parameters;

			this.startTime = DateTime.Now;
			this.durationSecs = 0.0f;

			this.thread = new Thread(ThreadMain);
		}
		
		public void Run()
		{
			this.thread.Start();
		}
		
		public void Finalise()
		{
			foreach (RawHull rawHull in rawHulls)
			{
				Mesh mesh = new Mesh();
				mesh.vertices = rawHull.vertices;
				mesh.triangles = rawHull.indices;
				outputHulls.Add(mesh);
			}

			this.durationSecs = (float)(DateTime.Now - startTime).TotalSeconds;

			this.thread = null;
		}

		private unsafe void ThreadMain()
		{
			var vhacd = VhacdInterface.CreateVHACD();

			RawVhacdParameters rawParameters = new RawVhacdParameters(parameters);

			fixed (Vector3* pVertices = inputVertices)
			fixed (int* pIndices = inputIndices)
			{
				VhacdInterface.ComputeFloat(vhacd,
											(float*)pVertices, (uint)inputVertices.Length,
											(uint*)pIndices, (uint)inputIndices.Length / 3,
											ref rawParameters);
			}

			// Fetch output data

			uint numHulls = VhacdInterface.GetNConvexHulls(vhacd);
			foreach (int index in Enumerable.Range(0, (int)numHulls))
			{
				ConvexHull hull;
				VhacdInterface.GetConvexHull(vhacd, (uint)index, &hull);

				RawHull rawHull = new RawHull();

				Vector3[] hullVerts = new Vector3[hull.m_nPoints];

				fixed (Vector3* pHullVerts = hullVerts)
				{
					double* pComponents = hull.m_points;
					Vector3* pVerts = pHullVerts;

					for (var pointCount = hull.m_nPoints; pointCount != 0; --pointCount)
					{
						pVerts->x = (float)pComponents[0];
						pVerts->y = (float)pComponents[1];
						pVerts->z = (float)pComponents[2];

						pVerts += 1;
						pComponents += 3;
					}
				}
				
				rawHull.vertices = hullVerts;

				int[] indices = new int[hull.m_nTriangles * 3];
				Marshal.Copy((System.IntPtr)hull.m_triangles, indices, 0, indices.Length);
				rawHull.indices = indices;
				
				rawHulls.Add(rawHull);
			}

			VhacdInterface.DestroyVHACD(vhacd);

			isFinished = true;
		}

		public bool IsFinished()
		{
			return isFinished;
		}
	}
}