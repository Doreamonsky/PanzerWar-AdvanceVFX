
using UnityEngine;
using System.Collections.Generic;

namespace Technie.PhysicsCreator
{
	public enum HullType
	{
		Box,
		ConvexHull,
		Sphere,
		Face,
		FaceAsBox,
		Auto
	}

	public enum AutoHullPreset
	{
		Low,
		Medium,
		High,
		Placebo,
		Custom
	}

	[System.Serializable]
	public class Hull
	{
		public string name = "<unnamed hull>";

		public bool isVisible = true;

		public HullType type = HullType.ConvexHull;
		
		public Color colour = Color.white;

		public PhysicMaterial material;

		public bool enableInflation = false;
		public float inflationAmount = 0.01f;

		public BoxFitMethod boxFitMethod = BoxFitMethod.MinimumVolume;

		public bool isTrigger = false;
		public bool isChildCollider = false;
		
		public List<int> selectedFaces = new List<int> ();

		// Convex hull data
		public Mesh collisionMesh; // Computed convex hull. Reference to the stored mesh asset in HullData
		
		// Box data
		public Bounds collisionBox; // The computed box data
		public Vector3 boxPosition; // The local position for the Box collider (when used as a child object)
		public Quaternion boxRotation; // The local rotation for the Box collider (when used as a child object)

		// Sphere data
		public Sphere collisionSphere; // The computed sphere data

		// Face data
		public Mesh faceCollisionMesh; // Computed mesh for face hull. Reference to the stored mesh asset in HullData

		// FaceAsBox data
		public Vector3 faceBoxCenter, faceBoxSize; // The computed FaceAsBox data
		public Quaternion faceAsBoxRotation; // The local rotation for the FaceAsBox

		// Auto hulls
		public Mesh[] autoMeshes = new Mesh[0];

		public bool hasColliderError;
		public int numColliderFaces;

		public void Destroy() {}

		public bool ContainsAutoMesh(Mesh m)
		{
			if (autoMeshes != null)
			{
				foreach (Mesh autoMesh in autoMeshes)
				{
					if (autoMesh == m)
						return true;
				}
			}
			return false;
		}
	}

	public class PaintingData : ScriptableObject
	{
		public readonly Color[] hullColours = new Color[]
		{
			new Color(0.0f, 1.0f, 1.0f, 0.7f),
			new Color(1.0f, 0.0f, 1.0f, 0.7f),
			new Color(1.0f, 1.0f, 0.0f, 0.7f),
			
			new Color(1.0f, 0.0f, 0.0f, 0.7f),
			new Color(0.0f, 1.0f, 0.0f, 0.7f),
			new Color(0.0f, 0.0f, 1.0f, 0.7f),

			new Color(1.0f, 1.0f, 1.0f, 0.7f),

			new Color(1.0f, 0.5f, 0.0f, 0.7f),
			new Color(1.0f, 0.0f, 0.5f, 0.7f),
			new Color(0.5f, 1.0f, 0.0f, 0.7f),
			new Color(0.0f, 1.0f, 0.5f, 0.7f),
			new Color(0.5f, 0.0f, 1.0f, 0.7f),
			new Color(0.0f, 0.5f, 1.0f, 0.7f),
		};

		public int TotalOutputColliders
		{
			get
			{
				int total = 0;
				foreach (Hull h in hulls)
				{
					if (h.type == HullType.Auto)
					{
						total += (h.autoMeshes != null) ? h.autoMeshes.Length : 0;
					}
					else
					{
						total++;
					}
				}
				return total;
			}
		}

		// Serialised Data

		public HullData hullData;

		public Mesh sourceMesh;

		public int activeHull = -1;

		public float faceThickness = 0.1f;

		public List<Hull> hulls = new List<Hull>();

		public AutoHullPreset autoHullPreset = AutoHullPreset.Medium;
		public VhacdParameters vhacdParams = new VhacdParameters();

		public bool hasLastVhacdTimings = false;
		public AutoHullPreset lastVhacdPreset = AutoHullPreset.Medium;
		public float lastVhacdDurationSecs = 0.0f;

		public void AddHull(HullType type, PhysicMaterial material, bool isChild, bool isTrigger)
		{
			hulls.Add( new Hull() );
			
			// Name the new hull
			hulls [hulls.Count - 1].name = "Hull " + hulls.Count;
			
			// Set selection to new hull
			activeHull = hulls.Count - 1;
			
			// Set the colour for the new hull
			hulls[hulls.Count-1].colour = hullColours[ activeHull % hullColours.Length ];
			hulls[hulls.Count-1].type = type;
			hulls[hulls.Count-1].material = material;
			hulls[hulls.Count-1].isTrigger = isTrigger;
			hulls[hulls.Count-1].isChildCollider = isChild;
		}

		public void RemoveHull (int index)
		{
			hulls [index].Destroy ();
			hulls.RemoveAt (index);
		}

		public void RemoveAllHulls ()
		{
			for (int i = 0; i < hulls.Count; i++)
			{
				hulls[i].Destroy();
			}
			hulls.Clear();
		}

		public bool HasActiveHull()
		{
			return activeHull >= 0 && activeHull < hulls.Count;
		}
		
		public Hull GetActiveHull()
		{
			if (activeHull < 0 || activeHull >= hulls.Count)
				return null;
			
			return hulls [activeHull];
		}

		public void GenerateCollisionMesh(Hull hull, Vector3[] meshVertices, int[] meshIndices, Mesh[] autoHulls)
		{
			hull.hasColliderError = false;

			if (hull.type == HullType.Box)
			{
				if (hull.selectedFaces.Count > 0)
				{
					if (hull.isChildCollider)
					{
						if (hull.boxFitMethod == BoxFitMethod.MinimumVolume)
						{
							RotatedBoxFitter fitter = new RotatedBoxFitter();
							fitter.Fit(hull, meshVertices, meshIndices);
						}
						else if (hull.boxFitMethod == BoxFitMethod.AlignFaces)
						{
							FaceAlignmentBoxFitter fitter = new FaceAlignmentBoxFitter();
							fitter.Fit(hull, meshVertices, meshIndices);
						}
						else if (hull.boxFitMethod == BoxFitMethod.AxisAligned)
						{
							AxisAlignedBoxFitter fitter = new AxisAlignedBoxFitter();
							fitter.Fit(hull, meshVertices, meshIndices);
						}
					}
					else
					{
						// Not a child, so we can only do an axis-aligned box
						// Calculate the minimum span on each axis

						Vector3 first = meshVertices[meshIndices[hull.selectedFaces[0] * 3]];

						Vector3 min = first;
						Vector3 max = first;

						for (int i = 0; i < hull.selectedFaces.Count; i++)
						{
							int faceIndex = hull.selectedFaces[i];

							Vector3 p0 = meshVertices[meshIndices[faceIndex * 3]];
							Vector3 p1 = meshVertices[meshIndices[faceIndex * 3 + 1]];
							Vector3 p2 = meshVertices[meshIndices[faceIndex * 3 + 2]];

							Inflate(p0, ref min, ref max);
							Inflate(p1, ref min, ref max);
							Inflate(p2, ref min, ref max);
						}

						hull.collisionBox.center = (min + max) * 0.5f;
						hull.collisionBox.size = max - min;
						hull.boxRotation = Quaternion.identity;
					}
				}
			}
			else if (hull.type == HullType.Sphere)
			{
				if (hull.collisionSphere == null)
				{
					hull.collisionSphere = new Sphere();
				}

				Vector3 sphereCenter;
				float sphereRadius;
				if (CalculateBoundingSphere(hull, meshVertices, meshIndices, out sphereCenter, out sphereRadius))
				{
					hull.collisionSphere.center = sphereCenter;
					hull.collisionSphere.radius = sphereRadius;
				}
				else
				{
					hull.collisionSphere.center = Vector3.zero;
					hull.collisionSphere.radius = 0.0f;
				}
			}
			else if (hull.type == HullType.ConvexHull)
			{
				if (hull.collisionMesh == null)
				{
					hull.collisionMesh = new Mesh();
				}

				hull.collisionMesh.name = hull.name;

				hull.collisionMesh.triangles = new int[0];
				hull.collisionMesh.vertices = new Vector3[0];

				GenerateConvexHull(hull, meshVertices, meshIndices, hull.collisionMesh);
			}
			else if (hull.type == HullType.Face)
			{
				if (hull.faceCollisionMesh == null)
				{
					hull.faceCollisionMesh = new Mesh();
				}

				hull.faceCollisionMesh.name = hull.name;

				hull.faceCollisionMesh.triangles = new int[0];
				hull.faceCollisionMesh.vertices = new Vector3[0];

				GenerateFace(hull, meshVertices, meshIndices, faceThickness);
			}
			else if (hull.type == HullType.FaceAsBox)
			{
				if (hull.selectedFaces.Count > 0)
				{
					if (hull.isChildCollider)
					{
						// Calculate the plane the vertices lie on

						Vector3[] vertices = ExtractUniqueVertices(hull, meshVertices, meshIndices);

						Vector3 primaryAxis = CalcPrimaryAxis(hull, meshVertices, meshIndices, !hull.isChildCollider);
						Vector3 tempUp = Vector3.Dot(primaryAxis, Vector3.up) > 0.8f ? Vector3.right : Vector3.up;
						Vector3 tempRight = Vector3.Cross(primaryAxis, tempUp);
						Vector3 primaryUp = Vector3.Cross(primaryAxis, tempRight);

						// Now find the best rotation of a rectangle on our plane that fits tightest to our points
						// We'll scan a range of angles and keep the best one (ie. the one with the smallest area)

						float bestAngle = 0.0f;
						float bestArea = float.MaxValue;
						Vector3 bestMin = Vector3.zero;
						Vector3 bestMax = Vector3.zero;
						Quaternion bestRotation = Quaternion.identity;

						float coarseInc = 5.0f;     // Coarse scan is run at 5 degree increments
						float precisionInc = 0.05f; // Precision scan in sub-degree increments

						// Scan at a fairly coarse increment to find a good ballpark angle
						for (float angle = 0.0f; angle <= 360.0f; angle += coarseInc)
						{
							Vector3 min, max;
							Quaternion outRotation;
							float area = CalcRequiredArea(angle, primaryAxis, primaryUp, vertices, out min, out max, out outRotation);
							if (area < bestArea)
							{
								bestAngle = angle;
								bestArea = area;
								bestMin = min;
								bestMax = max;
								bestRotation = outRotation;
							}
						}

						// Run a second detailed scan around the ballpark angle to refine it further
						float refineStartAngle = bestAngle - coarseInc;
						float refineEndAngle = bestAngle + coarseInc;
						for (float angle = refineStartAngle; angle <= refineEndAngle; angle += precisionInc)
						{
							Vector3 min, max;
							Quaternion outRotation;
							float area = CalcRequiredArea(angle, primaryAxis, primaryUp, vertices, out min, out max, out outRotation);
							if (area < bestArea)
							{
								bestAngle = angle;
								bestArea = area;
								bestMin = min;
								bestMax = max;
								bestRotation = outRotation;
							}
						}

						// Calculate the child box's center and size
						Vector3 center = (bestMin + bestMax) / 2.0f;
						Vector3 size = bestMax - bestMin;

						// Increase the size so we're always at least faceThickness deep
						float extraDepth = size.z - faceThickness;
						center.z += extraDepth * 0.5f;
						size.z += extraDepth;

						hull.faceBoxCenter = center;
						hull.faceBoxSize = size;
						hull.faceAsBoxRotation = bestRotation;
					}
					else
					{
						Vector3[] vertices = ExtractUniqueVertices(hull, meshVertices, meshIndices);
						Vector3 primaryAxis = CalcPrimaryAxis(hull, meshVertices, meshIndices, !hull.isChildCollider);

						Vector3 first = vertices[0];

						Vector3 min = first;
						Vector3 max = first;

						foreach (Vector3 v in vertices)
						{
							Inflate(v, ref min, ref max);
						}

						Vector3 center = (min + max) / 2.0f;
						Vector3 size = (max - min);

						if (Mathf.Abs(primaryAxis.x) > 0.0f)
						{
							float sign = primaryAxis.x > 0 ? +1.0f : -1.0f;

							float extraWidth = size.x - faceThickness;
							center.x += extraWidth * 0.5f * sign;
							size.x += extraWidth;
						}
						else if (Mathf.Abs(primaryAxis.y) > 0.0f)
						{
							float sign = primaryAxis.y > 0 ? +1.0f : -1.0f;

							float extraHeight = size.y - faceThickness;
							center.y += extraHeight * 0.5f * sign;
							size.y += extraHeight;
						}
						else
						{
							float sign = primaryAxis.z > 0 ? +1.0f : -1.0f;

							float extraDepth = size.z - faceThickness;
							center.z += extraDepth * 0.5f * sign;
							size.z += extraDepth;
						}

						hull.faceBoxCenter = center;
						hull.faceBoxSize = size;
						hull.faceAsBoxRotation = Quaternion.identity;
					}
				}
			}
			else if (hull.type == HullType.Auto)
			{
				// Generate a convex hull from the painted faces to use as a bounds

				if (hull.collisionMesh == null)
				{
					hull.collisionMesh = new Mesh();
				}

				hull.collisionMesh.name = string.Format("{0} bounds", hull.name);

				hull.collisionMesh.triangles = new int[0];
				hull.collisionMesh.vertices = new Vector3[0];

				GenerateConvexHull(hull, meshVertices, meshIndices, hull.collisionMesh);

				// Clip all of the auto hulls against the bounding hull

				List<Mesh> clippedHulls = new List<Mesh>();

				if (hull.selectedFaces.Count == (this.sourceMesh.triangles.Length / 3))
				{
					clippedHulls.AddRange(autoHulls);
				}
				else
				{
					// Clip the auto hulls to our painted bounds
					foreach (Mesh auto in autoHulls)
					{
						// Clip

						Mesh clipped = Clip(hull.collisionMesh, auto);
						if (clipped != null)
						{
							clippedHulls.Add(clipped);
						}
					}
				}

				// Reassign names to the meshes now we have the final amount
				for (int i=0; i<clippedHulls.Count; i++)
				{
					clippedHulls[i].name = string.Format("{0}.{1}", hull.name, (i + 1));
				}

				// We have a set of new meshes (clippedHulls) and a set of existing meshes (autoMeshes)
				// We want to reuse the existing ones where possible so that existing references stay in sync
				// Get the current autoMeshes, and add/remove meshes until we have the correct amount
				// Then sync them with our new meshes so we reuse the old but with new data

				List<Mesh> finalMeshes = new List<Mesh>();
				if (hull.autoMeshes != null)
					finalMeshes.AddRange(hull.autoMeshes);
				while (finalMeshes.Count > clippedHulls.Count)
					finalMeshes.RemoveAt(finalMeshes.Count - 1);
				while (finalMeshes.Count < clippedHulls.Count)
					finalMeshes.Add(new Mesh());

				// Now sync up the final meshes with the updated clipped mesh data

				for (int i=0; i<clippedHulls.Count; i++)
				{
					finalMeshes[i].Clear();
					finalMeshes[i].name = clippedHulls[i].name;
					finalMeshes[i].vertices = clippedHulls[i].vertices;
					finalMeshes[i].triangles= clippedHulls[i].triangles;
				}

				// Save the final meshes in the Hull (will be added to the asset later)
				hull.autoMeshes = finalMeshes.ToArray();
			}
		}

		private Mesh Clip(Mesh boundingMesh, Mesh inputMesh)
		{
			if (boundingMesh == null || boundingMesh.triangles.Length == 0)
				return null;
			if (inputMesh == null || inputMesh.triangles.Length == 0)
				return null;
			
			CuttableMesh inProgress = new CuttableMesh(inputMesh);

			MeshCutter cutter = new MeshCutter();

			// Convert the bounding mesh to planes that we'll clip against
			Plane[] boundingPlanes = ConvertToPlanes(boundingMesh, false);

			// Clip the input mesh against the planes one at a time
			// Note that after each clip we must recreate the convex hull to fill the hole created in the cut plane
			// If we don't do that then further clipping can give inconsistant results based on the triangulation

			for (int i = 0; i < boundingPlanes.Length; i++)
			{
				Plane plane = boundingPlanes[i];

				cutter.Cut(inProgress, plane);

				Mesh cutOutput = cutter.GetBackOutput().CreateMesh();
				Mesh newHull = QHullUtil.FindConvexHull("", cutOutput, false);
				inProgress = new CuttableMesh(newHull);
			}

			Mesh resultMesh = inProgress.CreateMesh();
			if (resultMesh.triangles.Length > 0)
			{
				return resultMesh;
			}
			else
				return null;
		}

		private Plane[] ConvertToPlanes(Mesh convexMesh, bool show)
		{
			List<Plane> planes = new List<Plane>();

			Vector3[] vertices = convexMesh.vertices;
			int[] indices = convexMesh.triangles;

			for (int i = 0; i < indices.Length; i += 3)
			{
				Vector3 p0 = vertices[indices[i]];
				Vector3 p1 = vertices[indices[i + 1]];
				Vector3 p2 = vertices[indices[i + 2]];

				Vector3 e0 = (p1 - p0).normalized;
				Vector3 e1 = (p2 - p0).normalized;

				Vector3 normal = Vector3.Cross(e0, e1).normalized;

				if (normal.magnitude > 0.01f)
				{
					Plane plane = new Plane(normal, p0);
					if (!Contains(plane, planes))
					{
						planes.Add(plane);

						if (show)
						{
							GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
							obj.name = string.Format("{0} : {1} / {2} / {3}", i, indices[i], indices[i + 1], indices[i + 2]);
							obj.transform.SetPositionAndRotation(p0, Quaternion.LookRotation(normal));
						}
					}
				}
			}

			return planes.ToArray();
		}

		private static bool Contains(Plane toTest, List<Plane> planes)
		{
			foreach (Plane existing in planes)
			{
				if (Mathf.Abs(toTest.distance - existing.distance) < 0.01f
					&& Vector3.Angle(toTest.normal, existing.normal) < 0.01f)
				{
					return true;
				}
			}
			return false;
		}

		private Vector3 CalcPrimaryAxis(Hull hull, Vector3[] meshVertices, int[] meshIndices, bool snapToAxies)
		{
			int numNormals = 0;
			Vector3 cummulativeNormal = Vector3.zero;

			for (int i = 0; i < hull.selectedFaces.Count; i++)
			{
				int faceIndex = hull.selectedFaces[i];

				Vector3 p0 = meshVertices[meshIndices[faceIndex * 3]];
				Vector3 p1 = meshVertices[meshIndices[faceIndex * 3 + 1]];
				Vector3 p2 = meshVertices[meshIndices[faceIndex * 3 + 2]];

				Vector3 d0 = (p1 - p0).normalized;
				Vector3 d1 = (p2 - p0).normalized;
				Vector3 normal = Vector3.Cross(d0, d1);

				cummulativeNormal += normal;
				numNormals++;
			}
			
			Vector3 averageNormal = cummulativeNormal / (float)numNormals;

			if (averageNormal.magnitude < 0.0001f)
			{
				return Vector3.up;
			}
			else if (snapToAxies)
			{
				float dx = Mathf.Abs(averageNormal.x);
				float dy = Mathf.Abs(averageNormal.y);
				float dz = Mathf.Abs(averageNormal.z);

				if (dx > dy && dx > dz)
				{
					return new Vector3(averageNormal.x > 0.0 ? 1.0f : -1.0f, 0, 0);
				}
				else if (dy > dz)
				{
					return new Vector3(0, averageNormal.y > 0.0 ? 1.0f : -1.0f, 0);
				}
				else
				{
					return new Vector3(0, 0, averageNormal.z > 0.0 ? 1.0f : -1.0f);
				}
			}
			else
			{
				return averageNormal.normalized;
			}
		}

		/*
		private Vector3 CalcCenter(Vector3[] meshVertices)
		{
			Vector3 cummulativePos = Vector3.zero;

			for (int i=0; i<meshVertices.Length; i++)
			{
				cummulativePos += meshVertices[i];
			}

			Vector3 averagePos = cummulativePos / (float)meshVertices.Length;
			return averagePos;
		}
		*/

		private Vector3[] ExtractUniqueVertices(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			List<Vector3> vertices = new List<Vector3>();

			for (int i = 0; i < hull.selectedFaces.Count; i++)
			{
				int faceIndex = hull.selectedFaces[i];

				Vector3 p0 = meshVertices[meshIndices[faceIndex * 3]];
				Vector3 p1 = meshVertices[meshIndices[faceIndex * 3 + 1]];
				Vector3 p2 = meshVertices[meshIndices[faceIndex * 3 + 2]];

				if (!Contains(vertices, p0))
					vertices.Add(p0);

				if (!Contains(vertices, p1))
					vertices.Add(p1);

				if (!Contains(vertices, p2))
					vertices.Add(p2);
			}

			return vertices.ToArray();
		}

		private static bool Contains(List<Vector3> list, Vector3 p)
		{
			foreach (Vector3 existing in list)
			{
				float dist = Vector3.Distance(existing, p);
				if (dist < 0.0001f)
					return true;
			}
			return false;
		}

		private static float CalcRequiredArea(float angleDeg, Vector3 primaryAxis, Vector3 primaryUp, Vector3[] vertices, out Vector3 min, out Vector3 max, out Quaternion outBasis)
		{
			if (vertices.Length == 0)
			{
				min = Vector3.zero;
				max = Vector3.zero;
				outBasis = Quaternion.identity;
				return 0.0f;
			}

			// TODO!
			
			Quaternion offsetRotation = Quaternion.AngleAxis(angleDeg, primaryAxis);
			Vector3 activeUp = offsetRotation * primaryUp;
		//	Vector3 activeRight = Vector3.Cross(primaryAxis, activeUp);

			Quaternion activeBasis = Quaternion.LookRotation(primaryAxis, activeUp);
			Quaternion localToPlaneRot = Quaternion.Inverse(activeBasis);

			Vector3 first = localToPlaneRot * vertices[0];

			min = first;
			max = first;

			foreach (Vector3 v in vertices)
			{
				Vector3 pos = localToPlaneRot * v;
				Inflate(pos, ref min, ref max);
			}

			outBasis = activeBasis;

			Vector3 delta = max - min;
			float area = delta.x * delta.y; // Only calculate 2d area since the z-delta should be zero (or near zero)
			return area;
		}

		private bool CalculateBoundingSphere (Hull hull, Vector3[] meshVertices, int[] meshIndices, out Vector3 sphereCenter, out float sphereRadius)
		{
			if (hull.selectedFaces.Count == 0)
			{
				sphereCenter = Vector3.zero;
				sphereRadius = 0.0f;
				return false;
			}

			List<Vector3> points = new List<Vector3>();

			for (int i=0; i<hull.selectedFaces.Count; i++)
			{
				int faceIndex = hull.selectedFaces[i];
				
				Vector3 p0 = meshVertices[meshIndices[faceIndex * 3]];
				Vector3 p1 = meshVertices[meshIndices[faceIndex * 3 + 1]];
				Vector3 p2 = meshVertices[meshIndices[faceIndex * 3 + 2]];

				points.Add(p0);
				points.Add(p1);
				points.Add(p2);
			}

			Sphere s = SphereUtils.MinSphere(points);
			sphereCenter = s.center;
			sphereRadius = s.radius;

			return true;
		}

		private void GenerateConvexHull(Hull hull, Vector3[] meshVertices, int[] meshIndices, Mesh destMesh)
		{
			Vector3[] hullVertices;
			int[] hullIndices;

			QHullUtil.FindConvexHull(hull.name, hull.selectedFaces.ToArray(), meshVertices, meshIndices, out hullVertices, out hullIndices, true);

			hull.numColliderFaces = hullIndices.Length / 3;

			Debug.Log("Calculated collider for '" + hull.name + "' has " + hull.numColliderFaces + " faces");
			if (hull.numColliderFaces >= 256)
			{
				hull.hasColliderError = true;
				hull.enableInflation = true; // force inflation on to simplify this hull
			}

			// Push to collision mesh

			hull.collisionMesh.vertices = hullVertices;
			hull.collisionMesh.triangles = hullIndices;
			hull.collisionMesh.RecalculateBounds ();

			// Clear this as we're using a regular collision mesh now

			hull.faceCollisionMesh = null;
		}

		

		private void GenerateFace(Hull hull, Vector3[] meshVertices, int[] meshIndices, float thickness)
		{
			int totalFaces = hull.selectedFaces.Count;
			Vector3[] facePoints = new Vector3[totalFaces * 3 * 2];
			
			for (int i=0; i<hull.selectedFaces.Count; i++)
			{
				int faceIndex = hull.selectedFaces[i];
				
				Vector3 p0 = meshVertices[meshIndices[faceIndex * 3]];
				Vector3 p1 = meshVertices[meshIndices[faceIndex * 3 + 1]];
				Vector3 p2 = meshVertices[meshIndices[faceIndex * 3 + 2]];

				Vector3 d0 = (p1 - p0).normalized;
				Vector3 d1 = (p2 - p0).normalized;

				Vector3 normal = Vector3.Cross(d1, d0);

				int baseIndex = i * 3 * 2;

				facePoints[baseIndex]		= p0;
				facePoints[baseIndex + 1]	= p1;
				facePoints[baseIndex + 2]	= p2;
				
				facePoints[baseIndex + 3] = p0 + (normal * faceThickness);
				facePoints[baseIndex + 4] = p1 + (normal * faceThickness);
				facePoints[baseIndex + 5] = p2 + (normal * faceThickness);
			}

			int[] indices = new int[totalFaces * 3 * 2];
			for (int i=0; i<indices.Length; i++)
				indices [i] = i;

			// Push to collision mesh
			
			hull.faceCollisionMesh.vertices = facePoints;
			hull.faceCollisionMesh.triangles = indices;
			hull.faceCollisionMesh.RecalculateBounds ();

			// Clear this as we're using a face mesh now

			hull.collisionMesh = null;
		}
		
		public bool ContainsMesh(Mesh m)
		{
			foreach (Hull h in hulls)
			{
				if (h.collisionMesh == m)
					return true;
				if (h.faceCollisionMesh == m)
					return true;
				if (h.autoMeshes != null)
				{
					foreach (Mesh autoMesh in h.autoMeshes)
					{
						if (autoMesh == m)
							return true;
					}
				}
			}
			return false;
		}

		private static void Inflate(Vector3 point, ref Vector3 min, ref Vector3 max)
		{
			min.x = Mathf.Min(min.x, point.x);
			min.y = Mathf.Min(min.y, point.y);
			min.z = Mathf.Min(min.z, point.z);
			
			max.x = Mathf.Max(max.x, point.x);
			max.y = Mathf.Max(max.y, point.y);
			max.z = Mathf.Max(max.z, point.z);
		}

		public bool HasAutoHulls()
		{
			foreach (Hull h in hulls)
			{
				if (h.type == HullType.Auto)
					return true;
			}
			return false;
		}
	}

} // namespace Technie.PhysicsCreator

