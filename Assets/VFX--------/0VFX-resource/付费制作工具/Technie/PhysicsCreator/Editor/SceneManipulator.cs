using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

namespace Technie.PhysicsCreator
{
	public enum PickMode
	{
		Additive,
		Subtractive,
		Undecided
	}

	public class SceneManipulator
	{
		private HullPainter currentHullPainter;

		private ToolSelection currentToolSelection = ToolSelection.TrianglePainting;
		private PaintingBrush currentBrushSize = PaintingBrush.Precise;

		private bool isSelectingFaces;
		private PickMode pickMode;
		private Material overlayMaterial;

		// Pick clone
		private GameObject pickClone;
		private MeshFilter targetMeshFilter;
		private MeshCollider targetMeshCollider;
		private Vector3[] targetVertices;
		private int[] targetTriangles;

		// Rendering overlay
		private GameObject overlayObject;
		private MeshFilter overlayFilter;
		private MeshRenderer overlayRenderer;

		private GameObject overlayRoot;
		private List<Transform> overlayParents = new List<Transform>();

		// Debug
		private bool hideShadowHierarchy = true;
		private Ray lastPickRay;
		private RaycastHit lastRaycastHit;

		public SceneManipulator()
		{

		}

		public void Destroy()
		{
			InternalDestroy();
		}
		
		private void InternalDestroy()
		{
			// Destroy all temporary objects

			if (overlayObject != null)
			{
				GameObject.DestroyImmediate(overlayObject);
				overlayObject = null;
			}

			foreach (Transform t in overlayParents)
			{
				if (t != null)
					GameObject.DestroyImmediate(t.gameObject);
			}
			overlayParents.Clear();

			if (overlayRoot != null)
			{
				GameObject.DestroyImmediate(overlayRoot);
				overlayRoot = null;
			}

			if (pickClone != null)
			{
				GameObject.DestroyImmediate(pickClone);
				pickClone = null;
			}
		}

		public void DisconnectAssets (bool deleteChildComponents)
		{
			if (currentHullPainter != null)
			{
				Undo.SetCurrentGroupName ("Disconnect Hull Painter");

				// First delete child components if needed
				if (deleteChildComponents)
				{
					foreach (HullPainterChild child in currentHullPainter.GetComponentsInChildren<HullPainterChild>())
					{
						Undo.DestroyObjectImmediate(child);
					}
				}

				// Then destroy the actual hull painter component
				Undo.DestroyObjectImmediate (currentHullPainter);

				Undo.IncrementCurrentGroup ();

				currentHullPainter = null;
			}
		}

		public ToolSelection GetCurrentTool()
		{
			return this.currentToolSelection;
		}

		public PaintingBrush GetCurrentBrush()
		{
			return this.currentBrushSize;
		}

		public void SetTool(ToolSelection sel)
		{
			this.currentToolSelection = sel;
		}

		public void SetBrush(PaintingBrush brush)
		{
			this.currentBrushSize = brush;
		}

		public int GetBrushPixelSize()
		{
			switch (currentBrushSize)
			{
				case PaintingBrush.Precise:
					{
						return 0;
					}
				case PaintingBrush.Small:
					{
						return 8;
					}
				case PaintingBrush.Medium:
					{
						return 20;
					}
				case PaintingBrush.Large:
					{
						return 32;
					}
			}
			return 0;
		}

		public bool DoMouseDown(PickMode initialMode)
		{
			if (currentToolSelection == ToolSelection.TrianglePainting)
			{
				if (targetMeshCollider != null)
				{
					if (currentHullPainter != null && currentHullPainter.paintingData != null)
					{
						Undo.RecordObject(currentHullPainter.paintingData, "Paint Hull");

						pickMode = PickTriangle(initialMode);
						if (pickMode != PickMode.Undecided)
						{
							//	Debug.Log ("Start drag");

							Sync();

							EditorUtility.SetDirty(currentHullPainter.paintingData);

							isSelectingFaces = true;

							return true;
						}
						else
						{
							//	Debug.Log ("Abandon drag");
						}
					}
					else
					{
						// This can happen when unity triggers scene callbacks in an odd order and the currentHullPainter isn't set yet
					//	Debug.LogError("SceneManipulator has no currentHullPainter!");
					}
				}
				else
				{
				//	Debug.Log("Mouse down but no targetMeshCollider, ignoring");
				}
			}
			else if (currentToolSelection == ToolSelection.Pipette)
			{
				// Raycast against the target mesh collider and see if the triangle we hit is in any current hull

				bool anyFound = false;

				Ray pickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

				int hitTriIndex = -1;
				if (targetMeshCollider != null && RaycastUtil.Raycast(targetMeshCollider, pickRay, out hitTriIndex, 10000.0f))
				{

					for (int i=0; i<currentHullPainter.paintingData.hulls.Count; i++)
					{
						Hull hull = currentHullPainter.paintingData.hulls[i];
						if (hull.selectedFaces.Contains(hitTriIndex))
						{
							// Now painting this hull!
							currentHullPainter.paintingData.activeHull = i;
							currentToolSelection = ToolSelection.TrianglePainting;
							anyFound = true;
							break;
						}
					}
				}

				if (!anyFound)
				{
					currentToolSelection = ToolSelection.TrianglePainting;
					currentHullPainter.paintingData.activeHull = -1;
				}
			}

			return false;
		}

		public bool DoMouseDrag()
		{
			if (isSelectingFaces)
			{
				Undo.RecordObject(currentHullPainter.paintingData, "Paint Hull");

				PickTriangle(pickMode);

				SyncOverlay(currentHullPainter);

				EditorUtility.SetDirty (currentHullPainter.paintingData);

				return true;
			}
			return false;
		}

		public bool DoMouseUp()
		{
			if (isSelectingFaces)
			{
				isSelectingFaces = false;
				return true;
			}
			return false;
		}

		private void DestroyClone()
		{
		//	Debug.Log ("DestroyClone");

			GameObject.DestroyImmediate(pickClone);

			pickClone = null;
			targetMeshFilter = null;
			targetMeshCollider = null;

			targetVertices = null;
			targetTriangles = null;

			isSelectingFaces = false;
		}

		public HullPainter GetCurrentHullPainter()
		{
			return currentHullPainter;
		}

		public bool Sync()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				// If we're in play mode then don't try and keep the overlay in sync
				// This also means we delete the overlay when entering playmode which stops unity getting confused about objects that shouldn't really exist
				InternalDestroy();
				return false;
			}
			
			HullPainter selectedHullPainter = SelectionUtil.FindSelectedHullPainter ();
			MeshFilter selectedMeshFilter = SelectionUtil.FindSelectedMeshFilter();

			if (selectedHullPainter != null)
			{
				SyncParentChain(selectedHullPainter.gameObject);

				FindOrCreateOverlay();

				FindOrCreatePickClone();

				SyncPickClone(selectedMeshFilter);

				SyncOverlay(selectedHullPainter);
			}
			else
			{
				if (pickClone != null)
				{
					targetMeshCollider.enabled = false;
				}
				
				if (overlayObject != null)
				{
					overlayRenderer.enabled = false;
				}
			}

			bool changed = false;
			if (currentHullPainter != selectedHullPainter)
			{
				currentHullPainter = selectedHullPainter;
				changed = true;
			}

			return changed;
		}

		private void FindOrCreatePickClone()
		{
			string cloneName = "RAYCAST TARGET (hull painter)";

			if (pickClone == null)
			{
				pickClone = GameObject.Find(cloneName);
			}

			if (pickClone != null)
			{
			//	Debug.Log("Use existing pick clone");

				targetMeshFilter = pickClone.GetComponent<MeshFilter>();
				targetMeshCollider = pickClone.GetComponent<MeshCollider>();
			}
			else
			{
			//	Debug.Log("Create new pick clone from scratch");

				pickClone = new GameObject (cloneName);

				if (hideShadowHierarchy)
					pickClone.hideFlags = HideFlags.HideAndDontSave;
				else
					pickClone.hideFlags = HideFlags.DontSave;

				targetMeshFilter = pickClone.AddComponent<MeshFilter> ();
				targetMeshCollider = pickClone.AddComponent<MeshCollider> ();
			}

			pickClone.transform.SetParent( overlayParents[ overlayParents.Count-1 ], false );
		}

		private void FindOrCreateOverlay()
		{
			string overlayName = "OVERLAY (hull painter)";

			if (overlayObject == null)
			{
				overlayObject = GameObject.Find(overlayName);
			}

			if (overlayObject != null)
			{
			//	Debug.Log ("Use existing overlay");

				overlayFilter = overlayObject.GetComponent<MeshFilter> ();
				overlayRenderer = overlayObject.GetComponent<MeshRenderer> ();
			}
			else
			{
			//	Debug.Log ("Create new overlay from scratch");

				overlayObject = new GameObject (overlayName);
				overlayObject.transform.localPosition = Vector3.zero;
				overlayObject.transform.localRotation = Quaternion.identity;
				overlayObject.transform.localScale = Vector3.one;

				if (hideShadowHierarchy)
					overlayObject.hideFlags = HideFlags.HideAndDontSave;
				else
					overlayObject.hideFlags = HideFlags.None;

				overlayFilter = overlayObject.AddComponent<MeshFilter> ();
				overlayRenderer = overlayObject.AddComponent<MeshRenderer> ();

				overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				overlayRenderer.receiveShadows = false;

				overlayFilter.sharedMesh = new Mesh ();

				Shader overlayShader = Shader.Find("Hidden/HullPainterOverlay");
				overlayMaterial = new Material(overlayShader);
				overlayRenderer.sharedMaterial = overlayMaterial;
			}

			overlayObject.transform.SetParent( overlayParents[ overlayParents.Count-1 ], false );
		}

		public void PaintAllFaces()
		{
			if (targetMeshCollider == null || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			Hull hull = currentHullPainter.paintingData.GetActiveHull();
			if (hull != null)
			{
				hull.selectedFaces.Clear();
				int numTris = targetMeshCollider.sharedMesh.triangles.Length / 3;
				for (int i = 0; i < numTris; i++)
				{
					hull.selectedFaces.Add(i);
				}
			}
		}

		public void UnpaintAllFaces()
		{
			if (targetMeshCollider == null || currentHullPainter == null || currentHullPainter.paintingData == null)
				return;

			Hull hull = currentHullPainter.paintingData.GetActiveHull();
			if (hull != null)
			{
				hull.selectedFaces.Clear();
			}
		}

		private PickMode PickTriangle(PickMode pickMode)
		{
			if (Camera.current == null)
				return PickMode.Undecided;

			Ray pickRay = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);

			int hitTriIndex = -1;
			if (RaycastUtil.Raycast(targetMeshCollider, pickRay, out hitTriIndex, 10000.0f))
			{
				if (currentHullPainter.paintingData.HasActiveHull())
				{
					Hull hull = currentHullPainter.paintingData.GetActiveHull();
					if (pickMode == PickMode.Additive)
					{
						if (!hull.selectedFaces.Contains(hitTriIndex))
							hull.selectedFaces.Add(hitTriIndex);

						PickArea(targetMeshCollider, hull, Event.current.mousePosition, true);

						return PickMode.Additive;
					}
					else if (pickMode == PickMode.Subtractive)
					{
						hull.selectedFaces.Remove(hitTriIndex);

						PickArea(targetMeshCollider, hull, Event.current.mousePosition, false);

						return PickMode.Subtractive;
					}
					else if (pickMode == PickMode.Undecided)
					{
						if (hull.selectedFaces.Contains(hitTriIndex))
						{
							hull.selectedFaces.Remove(hitTriIndex);

							PickArea(targetMeshCollider, hull, Event.current.mousePosition, false);

							return PickMode.Subtractive;
						}
						else
						{
							hull.selectedFaces.Add(hitTriIndex);

							PickArea(targetMeshCollider, hull, Event.current.mousePosition, true);

							return PickMode.Additive;
						}
					}
				}
			}

			return PickMode.Undecided;
		}

		private void PickArea (MeshCollider targetMeshCollider, Hull hull, Vector2 clickPos, bool asAdditive)
		{
			int range = GetBrushPixelSize();
			int numRays = range + 1;

			for (int i = 0; i < numRays; i++)
			{
				Vector2 jitteredPos;

				if (i == 0)
					jitteredPos = clickPos;
				else
					jitteredPos = new Vector2 (clickPos.x + Random.Range (-range, range), clickPos.y + Random.Range (-range, range));

				Ray pickRay = HandleUtility.GUIPointToWorldRay (jitteredPos);

				int hitTriIndex = -1;
				if (RaycastUtil.Raycast (targetMeshCollider, pickRay, out hitTriIndex, 10000.0f))
				{
					if (asAdditive)
					{
						if (!hull.selectedFaces.Contains (hitTriIndex))
							hull.selectedFaces.Add (hitTriIndex);
					}
					else
					{
						hull.selectedFaces.Remove(hitTriIndex);
					}
				}
			}
		}

		private void SyncPickClone(MeshFilter selectedMeshFilter)
		{
			if (selectedMeshFilter != null)
			{
				Mesh mesh = selectedMeshFilter.sharedMesh;

				targetMeshFilter.sharedMesh = mesh;
				targetMeshCollider.sharedMesh = mesh;

				targetVertices = mesh.vertices;
				targetTriangles = mesh.triangles;

				targetMeshCollider.enabled = true;
			}
			else
			{
				targetMeshCollider.enabled = false;
			}
		}

		private static float CalcTriangleArea(Vector3 p0, Vector3 p1, Vector3 p2)
		{
			Vector3 d0 = p1 - p0;
			Vector3 d1 = p2 - p0;

			float area = 0.5f * Vector3.Cross(d0, d1).magnitude;
			return area;
		}

		private void SyncOverlay(HullPainter hullPainter)
		{
		//	Debug.Log ("SyncOverlay - overlayObject: " + overlayObject);

			if (hullPainter != null && hullPainter.paintingData != null)
			{
				int totalFaces = 0;
				for (int i=0; i<hullPainter.paintingData.hulls.Count; i++)
				{
					Hull hull = hullPainter.paintingData.hulls[i];
					if (hull.isVisible)
					{
						totalFaces += hull.selectedFaces.Count;
					}
				}

		//		Debug.Log("Overlay has "+totalFaces+" faces");


				Vector3[] vertices = new Vector3[totalFaces * 3];
				Color[] colours = new Color[totalFaces * 3];
				int[] indices = new int[totalFaces * 3];

				// Rebuild vertex buffers

				int nextIndex = 0;

				for (int i=0; i<hullPainter.paintingData.hulls.Count; i++)
				{
					Hull hull = hullPainter.paintingData.hulls[i];

					if (!hull.isVisible)
						continue;

					Color baseColour = hull.colour;
					if (HullPainterWindow.instance.ShouldDimInactiveHulls() && hullPainter.paintingData.HasActiveHull())
					{
						if (hullPainter.paintingData.GetActiveHull() != hull)
						{
							float factor = 1.0f - HullPainterWindow.instance.GetInactiveHullDimFactor();
							baseColour.a = baseColour.a * factor;
						}
					}

					for (int j=0; j<hull.selectedFaces.Count; j++)
					{
						int faceIndex = hull.selectedFaces[j];

						int i0 = faceIndex * 3;
						int i1 = faceIndex * 3 + 1;
						int i2 = faceIndex * 3 + 2;

						if (i0 < targetTriangles.Length
							&& i1 < targetTriangles.Length
							&& i2 < targetTriangles.Length)
						{
							int t0 = targetTriangles[i0];
							int t1 = targetTriangles[i1];
							int t2 = targetTriangles[i2];

							if (t0 < targetVertices.Length && t1 < targetVertices.Length && t2 < targetVertices.Length)
							{
								Vector3 p0 = targetVertices[t0];
								Vector3 p1 = targetVertices[t1];
								Vector3 p2 = targetVertices[t2];

								colours[nextIndex] = baseColour;
								colours[nextIndex + 1] = baseColour;
								colours[nextIndex + 2] = baseColour;

								vertices[nextIndex] = p0;
								vertices[nextIndex + 1] = p1;
								vertices[nextIndex + 2] = p2;

								nextIndex += 3;
							}
							else
							{
								Debug.LogWarning("Skipping face because vertex index out of bounds");
							}
						}
						else
						{
							Debug.LogWarning("Skipping face because triangle index out of bounds: " + faceIndex);

							// TODO: Should we advance nextIndex or not?
							// Maybe we should have a validate step rather than trying to solve this problem now?
						}
					}
				}

				// Generate the indices
				for (int i=0; i<indices.Length; i++)
					indices [i] = i;

				Mesh overlayMesh = overlayFilter.sharedMesh;
				overlayMesh.triangles = new int[0];
				overlayMesh.vertices = vertices;
				overlayMesh.colors = colours;
				overlayMesh.triangles = indices;

				overlayFilter.sharedMesh = overlayMesh;

				overlayRenderer.enabled = true;
			}
			else
			{
				// No hull painter selected, clear everything

				overlayFilter.sharedMesh.Clear();
				overlayRenderer.enabled = false;
			}
		}

		private void SyncParentChain(GameObject srcLeafObj)
		{
			string rootName = "ROOT (hull painter)";

			if (overlayRoot == null)
			{
				overlayRoot = GameObject.Find(rootName);
			}

			if (overlayRoot != null)
			{
				// Found existing root, refind chain
				FindShadowChain(overlayRoot, overlayParents);
			}
			else
			{
				// Not found, create a new one from scratch
				overlayRoot = new GameObject(rootName);

				if (hideShadowHierarchy)
					overlayRoot.hideFlags = HideFlags.HideAndDontSave;
				else
					overlayRoot.hideFlags = HideFlags.None;
			}

			// Ensure the overaly is in the same scene as the object we're manipulating
			// (this also ensures the overaly is moved into the prefab editing scene when that is used)
			if (overlayRoot.scene != srcLeafObj.scene)
			{
				UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlayRoot, srcLeafObj.scene);
			}

			int depth = CalcTransformDepth(srcLeafObj);

			ResizeParentsList(overlayRoot.transform, overlayParents, depth); // !!

			SyncChainTransforms(srcLeafObj, overlayParents);
		}

		private static void FindShadowChain(GameObject shadowRoot, List<Transform> shadowChain)
		{
			// Clear chain list and try and repopulate it by matching game objects with SHADOW prefixes

			shadowChain.Clear();

			Transform current = shadowRoot.transform;
			do
			{
				// Bail if we don't have any children
				if (current.childCount == 0)
					break;

				// Fetch the first child
				current = current.GetChild(0);

				// Bail if it doesn't start with the correct prefix
				if (!current.name.StartsWith("SHADOW"))
					break;

				// Add this onto our chain
				shadowChain.Add(current);
			}
			while (true);
		}

		private static int CalcTransformDepth(GameObject obj)
		{
			int depth = 1;

			Transform parent = obj.transform.parent;
			while (parent != null)
			{
				parent = parent.parent;
				depth++;
			}
			return depth;
		}

		private static void ResizeParentsList(Transform shadowRoot, List<Transform> transforms, int newSize)
		{
			// NB: The next two steps shouldn't be nessisary, but users / unity can cause us
			// to have objects deleted out from under us
			// Sanity check and fix up our transforms list for safety

			// Strip out any null objects in the transforms list
			for (int i=transforms.Count-1; i>=0; i--)
			{
				if (transforms[i] == null)
					transforms.RemoveAt(i);
			}
			// Make sure the parenting is consistent
			for (int i=0; i<transforms.Count-1; i++)
			{
				transforms[i+1].SetParent( transforms[i], false );
			}

			// Add more leaf transforms if we're less than we need
			while (transforms.Count < newSize)
			{
				GameObject newObj = new GameObject();
				Transform parent = transforms.Count > 0 ? transforms[transforms.Count-1] : shadowRoot;
				newObj.transform.parent = parent;

				transforms.Add(newObj.transform);
			}

			// Remove leaf transforms if we have more than we need
			while (transforms.Count > newSize)
			{
				int lastIndex = transforms.Count - 1;
				GameObject.DestroyImmediate( transforms[lastIndex].gameObject );
				transforms.RemoveAt(lastIndex);
			}
		}

		private static void SyncChainTransforms(GameObject srcLeafObj, List<Transform> destTransforms)
		{
			Transform nextSrc = srcLeafObj.transform;
			Transform nextDest = destTransforms[ destTransforms.Count - 1 ];

			while (nextSrc != null)
			{
				nextDest.gameObject.name = "SHADOW ("+nextSrc.gameObject.name + ")";

				nextDest.localPosition = nextSrc.localPosition;
				nextDest.localRotation = nextSrc.localRotation;
				nextDest.localScale = nextSrc.localScale;

				nextSrc = nextSrc.parent;
				nextDest = nextDest.parent;
			}

		}

		public Vector3[] GetTargetVertices()
		{
			return targetVertices;
		}

		public int[] GetTargetTriangles()
		{
			return targetTriangles;
		}		

		public void DrawCustomCursor()
		{
			MouseCursor cursor = MouseCursor.Arrow;

			if (isSelectingFaces)
			{
				if (pickMode == PickMode.Additive)
					cursor = MouseCursor.ArrowPlus;

				if (pickMode == PickMode.Subtractive)
					cursor = MouseCursor.ArrowMinus;
			}
			else
			{
				if (Event.current.shift)
					cursor = MouseCursor.ArrowPlus;
				if (Event.current.control)
					cursor = MouseCursor.ArrowMinus;
			}
			
			Rect sceneRect = new Rect(0, 0, SceneView.lastActiveSceneView.position.width, SceneView.lastActiveSceneView.position.height);
			EditorGUIUtility.AddCursorRect(sceneRect, cursor);
		}
	}

} // namespace Technie.PhysicsCreator

