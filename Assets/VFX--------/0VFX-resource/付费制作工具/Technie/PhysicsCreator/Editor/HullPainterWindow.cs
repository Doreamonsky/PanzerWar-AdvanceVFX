
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

namespace Technie.PhysicsCreator
{
	public enum ToolSelection
	{
		TrianglePainting,
		Pipette
	}

	public enum PaintingBrush
	{
		Precise,
		Small,
		Medium,
		Large
	}

	[Flags]
	public enum Collumn
	{
		None				= 0,
		Visibility			= 1 << 1,
		Name				= 1 << 2,
		Colour				= 1 << 3,
		Type				= 1 << 4,
		Material			= 1 << 5,
		IsChild				= 1 << 6,
		Inflate				= 1 << 7,
		BoxFitMethod		= 1 << 8,
		Trigger				= 1 << 9,
		Paint				= 1 << 10,
		Delete				= 1 << 11,
		All					= ~0
	}

	public class GridRect
	{
		public int row;
		public Collumn col;
		public Rect rect;
	}

	public class HullPainterWindow : EditorWindow
	{
		// The actual install path with be detected at runtime with FindInstallPath
		// If for some reason that fails, the default install path will be used instead
		public const string defaultInstallPath = "Assets/Technie/PhysicsCreator/";

		private static readonly Collumn[] COLLUMN_ORDER = new Collumn[]
		{
			Collumn.Visibility,
			Collumn.Name,
			Collumn.Colour,
			Collumn.Type,
			Collumn.Material,
			Collumn.Inflate,
			Collumn.BoxFitMethod,
			Collumn.IsChild,
			Collumn.Trigger,
			Collumn.Paint,
			Collumn.Delete
		};

		private const float EXPONENTIAL_SCALE = 5.0f;

		private static bool isOpen;
		public static bool IsOpen() { return isOpen; }
		public static HullPainterWindow instance;

		private int activeMouseButton = -1;

		private bool repaintSceneView = false;
		private bool regenerateOverlay = false;
		private int hullToDelete = -1;

		private SceneManipulator sceneManipulator;

		// Foldout visibility
		private bool areToolsFoldedOut = true;
		private bool areHullsFoldedOut = true;
		private bool areSettingsFoldedOut = true;
		private bool areVhacdSettingsFoldedOut = true;
		private bool areErrorsFoldedOut = true;
		private bool areAssetsFoldedOut = true;

		private static Collumn visibleCollumns = Collumn.All;

		private Vector2 scrollPosition;

		private Texture addHullIcon;
		private Texture errorIcon;
		private Texture deleteIcon;
		private Texture deleteCollidersIcon;
		private Texture paintOnIcon;
		private Texture paintOffIcon;
		private Texture triggerOnIcon;
		private Texture triggerOffIcon;
		private Texture isChildIcon;
		private Texture nonChildIcon;
		private Texture preciseBrushIcon;
		private Texture smallBrushIcon;
		private Texture mediumBrushIcon;
		private Texture largeBrushIcon;
		private Texture pipetteIcon;
		private Texture hullVisibleIcon;
		private Texture hullInvisibleIcon;
		private Texture toolsIcons;
		private Texture hullsIcon;
		private Texture settingsIcon;
		private Texture assetsIcon;
		private Texture axisAlignedIcon;
		private Texture minimizeVolumeIcon;
		private Texture alignFacesIcon;
		private Texture generateIcon;
		private Texture paintAllIcon;
		private Texture paintNoneIcon;
		private Texture autoHullSettingsIcon;

		private HullType defaultType = HullType.ConvexHull;
		private PhysicMaterial defaultMaterial;
		private bool defaultIsChild;
		private bool defaultIsTrigger;

		private bool showWireframe = true;
		private float wireframeFactor = -1.0f;

		private bool dimInactiveHulls = true;
		private float dimFactor = 0.7f;

		private GUIStyle foldoutStyle;
		private Color dividerColour;
		
		private List<GridRect> gridRects = new List<GridRect>();

		[MenuItem("Window/Technie Collider Creator/Hull Painter", false, 1)]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow(typeof(HullPainterWindow));
		}

		void OnEnable()
		{
			string installPath = FindInstallPath();
			string iconsPath = installPath + "Icons/";

			dividerColour = new Color(116.0f / 255.0f, 116.0f / 255.0f, 116.0f / 255.0f);

			addHullIcon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "AddHullIcon.png");
			errorIcon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "ErrorIcon.png");
			deleteIcon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "DeleteIcon.png");
			deleteCollidersIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "DeleteCollidersIcon.png");

			paintOnIcon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "PaintOnIcon.png");
			paintOffIcon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "PaintOffIcon.png");

			triggerOnIcon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "TriggerOnIcon.png");
			triggerOffIcon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "TriggerOffIcon.png");

			isChildIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "TriggerOnIcon.png");
			nonChildIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "TriggerOffIcon.png");

			preciseBrushIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "PreciseBrushIcon.png");
			smallBrushIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "SmallBrushIcon.png");
			mediumBrushIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "MediumBrushIcon.png");
			largeBrushIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "LargeBrushIcon.png");

			pipetteIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "PipetteIcon.png");

			hullVisibleIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "HullVisibleIcon.png");
			hullInvisibleIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "HullInvisibleIcon.png");

			toolsIcons = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "ToolsIcon.png");
			hullsIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "HullIcon.png");
			settingsIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "SettingsIcon.png");
			assetsIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "AssetsIcon.png");

			axisAlignedIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "AxisAlignedIcon.png");
			minimizeVolumeIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "MinimizeVolumeIcon.png");
			alignFacesIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "AlignFacesIcon.png");
			generateIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "GenerateIcon.png");

			paintAllIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "PaintAllIcon.png");
			paintNoneIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "PaintNoneIcon.png");

			autoHullSettingsIcon = AssetDatabase.LoadAssetAtPath<Texture>(iconsPath + "AutoHullSettingsIcon.png");
			
			Texture icon = AssetDatabase.LoadAssetAtPath<Texture> (iconsPath + "TechnieIcon.png");
#if UNITY_5_0
			this.title = "Hull Painter";
#else
			this.titleContent = new GUIContent ("Hull Painter", icon, "Technie Hull Painter");
#endif

			sceneManipulator = new SceneManipulator();

			isOpen = true;
			instance = this;
		}

		void OnDestroy()
		{
#if UNITY_2019_1_OR_NEWER
			SceneView.beforeSceneGui -= this.OnBeforeSceneGUI;
			SceneView.duringSceneGui -= this.OnDuringSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#endif
			
			if (sceneManipulator != null)
			{
				sceneManipulator.Destroy();
				sceneManipulator = null;
			}

			isOpen = false;
			instance = null;
		}

		void OnFocus()
		{
			// Remove to make sure it's not added, then add it once
#if UNITY_2019_1_OR_NEWER
			SceneView.beforeSceneGui -= this.OnBeforeSceneGUI;
			SceneView.beforeSceneGui += this.OnBeforeSceneGUI;

			SceneView.duringSceneGui -= this.OnDuringSceneGUI;
			SceneView.duringSceneGui += this.OnDuringSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
			SceneView.onSceneGUIDelegate += this.OnSceneGUI;

			GizmoUtils.ToggleGizmos(true);
#endif
		}

		void OnSelectionChange()
		{
#if !UNITY_2019_1_OR_NEWER
			GizmoUtils.ToggleGizmos(true);
#endif

			if (sceneManipulator.Sync ())
			{
		//		Debug.Log ("Changed");
			}

			// Always repaint as we need to change inactive gui
			Repaint();
		}

		// Called from HullPainterEditor
		public void OnInspectorGUI()
		{
			if (sceneManipulator.Sync ())
			{
				Repaint();
			}
		}

		private void CreateStyles()
		{
			// Creating styles in OnEnable can throw NPEs if the window is docked
			// Instead it's more reliable to lazily init them just before we need them

			if (foldoutStyle == null)
			{
				foldoutStyle = new GUIStyle(EditorStyles.foldout);
				foldoutStyle.fontStyle = FontStyle.Bold;
			}
		}

		void OnGUI ()
		{
			// Only sync on layout so ui gets same calls
			if (Event.current.type == EventType.Layout)
			{
				sceneManipulator.Sync ();
			}

			CreateStyles();

			repaintSceneView = false;
			regenerateOverlay = false;
			hullToDelete = -1;

			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				DrawActiveGui(currentHullPainter);
			}
			else
			{
				DrawInactiveGui();
			}
		}

		/** Gui drawn if the selected object has a vaild hull painter and initialised asset data
		 */
		private void DrawActiveGui(HullPainter currentHullPainter)
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height));

			GUILayout.Space (10);
			
			DrawToolGui ();
			
			DrawHullGUI();

			DrawVhacdConfigGUI();

			DrawSettingsGui();
			
			DrawHullWarnings (currentHullPainter);

			DrawAssetGui();
			
			if (currentHullPainter.paintingData.hulls.Count == 0)
			{
				GUILayout.Label("No hulls created. Add a hull to start painting.");
			}

			GUILayout.Space (16);


			GUILayout.EndScrollView ();

			// Now actually perform queued up actions

			if (hullToDelete != -1)
			{
				Undo.RecordObject (currentHullPainter.paintingData, "Delete Hull");

				currentHullPainter.paintingData.RemoveHull (hullToDelete);

				EditorUtility.SetDirty (currentHullPainter.paintingData);
			}

			if (regenerateOverlay)
				sceneManipulator.Sync (); // may need to explicitly resync overlay data?

			if (repaintSceneView)
				SceneView.RepaintAll();
		}

		/** Gui drawn if the selected object does not have a valid and initialised hull painter on it
		 */
		private void DrawInactiveGui()
		{
			if (Selection.transforms.Length == 1)
			{
				// Have a single scene selection, is it viable?

				GameObject selectedObject = Selection.transforms[0].gameObject;
				MeshFilter srcMesh = SelectionUtil.FindSelectedMeshFilter();
				HullPainterChild child = SelectionUtil.FindSelectedHullPainterChild();

				if (srcMesh != null)
				{
					GUILayout.Space(10);
					GUILayout.Label("Generate an asset to start painting:");
					CommonUi.DrawGenerateOrReconnectGui(selectedObject, srcMesh.sharedMesh);
				}
				else if (child != null)
				{
					GUILayout.Space(10);
					GUILayout.Label("Child hulls are not edited directly - select the parent to continue painting this hull");
				}
				else
				{
					// No mesh filter, might have a hull painter (or not)

					GUILayout.Space(10);
					GUILayout.Label("To start painting, select a single scene object");
					GUILayout.Label("The object must contain a MeshFilter");

					GUILayout.Space(10);
					GUILayout.Label("No MeshFilter on selected object", EditorStyles.centeredGreyMiniLabel);
				}
			}
			else
			{
				// No single scene selection
				// Could be nothing selected
				// Could be multiple selection
				// Could be an asset in the project selected

				GUILayout.Space(10);
				GUILayout.Label("To start painting, select a single scene object");
				GUILayout.Label("The object must contain a MeshFilter");

				if (GUILayout.Button("Open quick start guide"))
				{
					string projectPath = Application.dataPath.Replace("Assets", "");
					string docsPdf = projectPath + FindInstallPath() + "Technie Collider Creator Readme.pdf";
					Application.OpenURL(docsPdf);
				}
			}
		}

		private void DrawToolGui()
		{
			areToolsFoldedOut = EditorGUILayout.Foldout(areToolsFoldedOut, new GUIContent("Tools", toolsIcons), foldoutStyle);
			if (areToolsFoldedOut)
			{
				GUILayout.BeginHorizontal();
				{
					ToolSelection currentToolSelection = sceneManipulator.GetCurrentTool();
					PaintingBrush currentBrushSize = sceneManipulator.GetCurrentBrush();

					Texture[] brushIcons = new Texture[] { preciseBrushIcon, smallBrushIcon, mediumBrushIcon, largeBrushIcon };
					int brushId = (currentToolSelection == ToolSelection.TrianglePainting) ? (int)currentBrushSize : -1;
					int newBrushId = GUILayout.Toolbar(brushId, brushIcons, UnityEditor.EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(22));

					int pipetteId = (currentToolSelection == ToolSelection.Pipette ? 0 : -1);
					int newPipetteId = GUILayout.Toolbar(pipetteId, new Texture[] { currentToolSelection == ToolSelection.Pipette ? pipetteIcon : pipetteIcon }, UnityEditor.EditorStyles.miniButton, GUILayout.Height(22), GUILayout.Width(30));

					if (newBrushId != brushId)
					{
						sceneManipulator.SetTool(ToolSelection.TrianglePainting);
						sceneManipulator.SetBrush((PaintingBrush)newBrushId);
					}
					else if (newPipetteId != pipetteId)
					{
						sceneManipulator.SetTool(newPipetteId == 0 ? ToolSelection.Pipette : ToolSelection.TrianglePainting);
					}
					
					if (GUILayout.Button(new GUIContent(paintAllIcon), GUILayout.Width(30)))
					{
						sceneManipulator.PaintAllFaces();
					}

					if (GUILayout.Button(new GUIContent(paintNoneIcon), GUILayout.Width(30)))
					{
						sceneManipulator.UnpaintAllFaces();
					}

					if (GUILayout.Button(new GUIContent("Generate", generateIcon), GUILayout.MinWidth(10)))
					{
						GenerateColliders();
					}
					
					if (GUILayout.Button(new GUIContent("Delete Colliders", deleteCollidersIcon), GUILayout.MinWidth(10)))
					{
						DeleteColliders();
					}

					if (GUILayout.Button(new GUIContent("Delete Generated", deleteCollidersIcon), GUILayout.MinWidth(10)))
					{
						DeleteGenerated();
					}
				}
				GUILayout.EndHorizontal();

			} // end foldout

			DrawUiDivider();
		}

		private void ClearGridLayout()
		{
			gridRects.Clear();
		}

		private void InsertCell(int row, Collumn col, Rect rect)
		{
			GridRect r = new GridRect();
			r.row = row;
			r.col = col;
			r.rect = rect;
			this.gridRects.Add(r);
		}

		private Rect GetCellRect(int row, Collumn col)
		{
			foreach (GridRect r in gridRects)
			{
				if (r.row == row && r.col == col)
					return r.rect;
			}
			return new Rect();
		}

		private void DrawHullGUI()
		{
			areHullsFoldedOut = EditorGUILayout.Foldout(areHullsFoldedOut, new GUIContent("Hulls", hullsIcon), foldoutStyle);
			if (areHullsFoldedOut)
			{
				HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();
				
				// Figure out collumn widths based on which are actually visible

				Dictionary<Collumn, float> collumnWidths = new Dictionary<Collumn, float>();
				collumnWidths.Add(Collumn.Visibility,		IsCollumnVisible(Collumn.Visibility) ? 45 : 0);
				collumnWidths.Add(Collumn.Colour,			IsCollumnVisible(Collumn.Colour) ? 45 : 0);
				collumnWidths.Add(Collumn.Type,				IsCollumnVisible(Collumn.Type) ? 80 : 0);
				collumnWidths.Add(Collumn.Inflate,			IsCollumnVisible(Collumn.Inflate) ? 12+40 : 0);
				collumnWidths.Add(Collumn.BoxFitMethod,		IsCollumnVisible(Collumn.BoxFitMethod) ? 60 : 0);
				collumnWidths.Add(Collumn.IsChild,			IsCollumnVisible(Collumn.IsChild) ? 55 : 0);
				collumnWidths.Add(Collumn.Trigger,			IsCollumnVisible(Collumn.Trigger) ? 45 : 0);
				collumnWidths.Add(Collumn.Paint,			IsCollumnVisible(Collumn.Paint) ? 40 : 0);
				collumnWidths.Add(Collumn.Delete,			IsCollumnVisible(Collumn.Delete) ? 45 : 0);

				float fixedWidth = 0;
				int numOptional = 0;
				foreach (float width in collumnWidths.Values)
				{
					fixedWidth += width;
					if (width > 0)
						numOptional++;
				}
				fixedWidth += 12; // extra space for window chrome
				fixedWidth += (numOptional * 4);
				if (IsCollumnVisible(Collumn.Material))
					fixedWidth += 4;
				
				float baseWidth = EditorGUIUtility.currentViewWidth;
				float flexibleWidth = baseWidth - fixedWidth;

				int numFlexible = 0;
				if (IsCollumnVisible(Collumn.Name))
					numFlexible++;
				if (IsCollumnVisible(Collumn.Material))
					numFlexible++;

				if (IsCollumnVisible(Collumn.Name))
					collumnWidths.Add(Collumn.Name, flexibleWidth / (float)numFlexible);
				else
					collumnWidths.Add(Collumn.Name, 0.0f);

				if (IsCollumnVisible(Collumn.Material))
					collumnWidths.Add(Collumn.Material, flexibleWidth / (float)numFlexible);
				else
					collumnWidths.Add(Collumn.Material, 0.0f);

				// Is there enough space (under optional collumns) to put the 'Add Hull' button inline, or does it need to go on a new line?
				bool putAddHullInline = IsCollumnVisible(Collumn.Name) || IsCollumnVisible(Collumn.Colour) || IsCollumnVisible(Collumn.Type) || IsCollumnVisible(Collumn.Material) || IsCollumnVisible(Collumn.Inflate) || IsCollumnVisible(Collumn.BoxFitMethod) || IsCollumnVisible(Collumn.IsChild) || IsCollumnVisible(Collumn.Trigger);

				// Build the grid of layout rects from the collumn widths

				ClearGridLayout();

				int numRows = currentHullPainter.paintingData.hulls.Count + 2; // +1 for collumn names, +1 for bottom buttons
				for (int row = 0; row < numRows; row++)
				{
					GUILayout.Label("");
					Rect rowRect = GUILayoutUtility.GetLastRect();

					float x = rowRect.x;
					for (int c=0; c < COLLUMN_ORDER.Length; c++)
					{
						Collumn col = COLLUMN_ORDER[c];

						if (IsCollumnVisible(col))
						{
							Rect gridRect = new Rect();
							gridRect.position = new Vector2(x, rowRect.y);
							gridRect.size = new Vector2(collumnWidths[col], rowRect.height);

							InsertCell(row, col, gridRect);

							x += collumnWidths[col] + 4;
						}
					}
				}

				// Collumn headings for the hull rows

				GUILayout.BeginHorizontal();
				{
					if (IsCollumnVisible(Collumn.Visibility))
						GUI.Label(GetCellRect(0, Collumn.Visibility), "Visible");

					if (IsCollumnVisible(Collumn.Name))
						GUI.Label(GetCellRect(0, Collumn.Name), "Name");

					if (IsCollumnVisible(Collumn.Colour))
						GUI.Label(GetCellRect(0, Collumn.Colour), "Colour");

					if (IsCollumnVisible(Collumn.Type))
						GUI.Label(GetCellRect(0, Collumn.Type), "Type");

					if (IsCollumnVisible(Collumn.Material))
						GUI.Label(GetCellRect(0, Collumn.Material), "Material");

					if (IsCollumnVisible(Collumn.Inflate))
						GUI.Label(GetCellRect(0, Collumn.Inflate), "Inflation");

					if (IsCollumnVisible(Collumn.BoxFitMethod))
						GUI.Label(GetCellRect(0, Collumn.BoxFitMethod), "Box Fit");

					if (IsCollumnVisible(Collumn.IsChild))
						GUI.Label(GetCellRect(0, Collumn.IsChild), "As Child");

					if (IsCollumnVisible(Collumn.Trigger))
						GUI.Label(GetCellRect(0, Collumn.Trigger), "Trigger");

					if (IsCollumnVisible(Collumn.Paint))
						GUI.Label(GetCellRect(0, Collumn.Paint), "Paint");

					if (IsCollumnVisible(Collumn.Delete))
						GUI.Label(GetCellRect(0, Collumn.Delete), "Delete");
				}
				GUILayout.EndHorizontal();

				// The actual hull rows with all the data for an individual hull

				for (int i = 0; i < currentHullPainter.paintingData.hulls.Count; i++)
				{
					DrawHullGUILine(i, currentHullPainter.paintingData.hulls[i]);
				}

				// The row of macro buttons at the bottom of each hull collumn (Show all, Delete all, etc.)

				GUILayout.BeginHorizontal();
				{
					if (IsCollumnVisible(Collumn.Visibility))
					{
						bool allHullsVisible = AreAllHullsVisible();
						if (GUI.Button(GetCellRect(numRows-1, Collumn.Visibility), new GUIContent(" All", allHullsVisible ? hullInvisibleIcon : hullVisibleIcon), EditorStyles.miniButton))
						{
							if (allHullsVisible)
								SetAllHullsVisible(false); // Hide all
							else
								SetAllHullsVisible(true); // Show all
						}
					}

					if (putAddHullInline)
					{
						// If we're drawing the Add Hull button inline, then we don't want it tied to a collumn like other buttons because we always want it to be visible
						// We also want it to span multiple collumns if it needs to
						// We calculate it's position manually, then draw it via GUI.Button (rather than GUILayout.Button) so that we don't interupt the auto-layout of the rest of the grid

						Collumn colToUse = Collumn.Name;
						for (int i=1; i<COLLUMN_ORDER.Length; i++)
						{
							if (IsCollumnVisible(COLLUMN_ORDER[i]))
							{
								colToUse = COLLUMN_ORDER[i];
								break;
							}
						}
						Rect addRect = GetCellRect(numRows - 1, colToUse);
						addRect.size = new Vector2(70.0f, addRect.size.y);

						if (GUI.Button(addRect, new GUIContent("Add Hull", addHullIcon), EditorStyles.miniButton))
						{
							AddHull();
						}
					}
					
					if (IsCollumnVisible(Collumn.Paint))
					{
						if (GUI.Button(GetCellRect(numRows-1, Collumn.Paint), "Stop", EditorStyles.miniButton))
						{
							StopPainting();
						}
					}

					if (IsCollumnVisible(Collumn.Delete))
					{
						if (GUI.Button(GetCellRect(numRows-1, Collumn.Delete), new GUIContent(" All", deleteIcon), EditorStyles.miniButton))
						{
							DeleteHulls();
						}
					}
				}
				GUILayout.EndHorizontal();

				// If we didn't draw the Add Hull button inline, then make a new row for it and draw it here
				if (!putAddHullInline)
				{
					GUILayout.BeginHorizontal();

					if (GUILayout.Button(new GUIContent("Add Hull", addHullIcon), EditorStyles.miniButton, GUILayout.Width(70.0f)))
					{
						AddHull();
					}

					GUILayout.EndHorizontal();
				}
			}
			DrawUiDivider();
		}

		private void DrawHullGUILine(int hullIndex, Hull hull)
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			int row = hullIndex + 1;

			Undo.RecordObject (currentHullPainter.paintingData, "Edit Hull");

			GUILayout.BeginHorizontal ();
			{
				if (IsCollumnVisible(Collumn.Visibility))
				{
					if (GUI.Button(GetCellRect(row, Collumn.Visibility), hull.isVisible ? hullVisibleIcon : hullInvisibleIcon, EditorStyles.miniButton))
						{
						hull.isVisible = !hull.isVisible;
						regenerateOverlay = true;
					}
				}

				if (IsCollumnVisible(Collumn.Name))
				{
					hull.name = EditorGUI.TextField(GetCellRect(row, Collumn.Name), hull.name);
				}


				if (IsCollumnVisible(Collumn.Colour))
				{
					Color prevColour = hull.colour;
					hull.colour = EditorGUI.ColorField(GetCellRect(row, Collumn.Colour), "", hull.colour);
					if (prevColour != hull.colour)
					{
						regenerateOverlay = true;
						repaintSceneView = true;
					}
				}

				if (IsCollumnVisible(Collumn.Type))
				{
					hull.type = (HullType)EditorGUI.EnumPopup(GetCellRect(row, Collumn.Type), hull.type);
				}

				if (IsCollumnVisible(Collumn.Material))
				{
					hull.material = (PhysicMaterial)EditorGUI.ObjectField(GetCellRect(row, Collumn.Material), hull.material, typeof(PhysicMaterial), false);
				}

				if (IsCollumnVisible(Collumn.Inflate))
				{
					Rect baseRect = GetCellRect(row, Collumn.Inflate);
					Rect toggleRect = new Rect(baseRect.position, new Vector2(14.0f, baseRect.size.y));
					Rect amountRect = new Rect(baseRect.position + new Vector2(16.0f, 0.0f), baseRect.size - new Vector2(16.0f, 0.0f));

					hull.enableInflation = EditorGUI.Toggle(toggleRect, hull.enableInflation);

					hull.inflationAmount = EditorGUI.FloatField(amountRect, hull.inflationAmount);
				}

				if (IsCollumnVisible(Collumn.BoxFitMethod))
				{
					if (hull.type == HullType.Box)
					{
						GUIContent[] options = new GUIContent[3];
						options[0] = new GUIContent("Axis", axisAlignedIcon);
						options[1] = new GUIContent("Tight", minimizeVolumeIcon);
						options[2] = new GUIContent("Face", alignFacesIcon);

						if (hull.isChildCollider)
						{
							int selected = EditorGUI.Popup(GetCellRect(row, Collumn.BoxFitMethod), (int)hull.boxFitMethod, options, EditorStyles.popup);
							hull.boxFitMethod = (BoxFitMethod)selected;
						}
						else
						{
							GUI.enabled = false;
							EditorGUI.Popup(GetCellRect(row, Collumn.BoxFitMethod), (int)BoxFitMethod.AxisAligned, options, EditorStyles.popup);
							GUI.enabled = true;
						}
					}
				}

				if (IsCollumnVisible(Collumn.IsChild))
				{
					if (GUI.Button(GetCellRect(row, Collumn.IsChild), hull.isChildCollider ? isChildIcon : nonChildIcon, EditorStyles.miniButton))
					{
						hull.isChildCollider = !hull.isChildCollider;
					}
				}

				if (IsCollumnVisible(Collumn.Trigger))
				{
					if (GUI.Button(GetCellRect(row, Collumn.Trigger), hull.isTrigger ? triggerOnIcon : triggerOffIcon, EditorStyles.miniButton))
					{
						hull.isTrigger = !hull.isTrigger;
					}
				}

				if (IsCollumnVisible(Collumn.Paint))
				{
					int prevHullIndex = currentHullPainter.paintingData.activeHull;

					bool isPainting = (currentHullPainter.paintingData.activeHull == hullIndex);
					int nowSelected = GUI.Toolbar(GetCellRect(row, Collumn.Paint), isPainting ? 0 : -1, new Texture[] { isPainting ? paintOnIcon : paintOffIcon }, EditorStyles.miniButton);
					if (nowSelected == 0 && prevHullIndex != hullIndex)
					{
						// Now painting this index!
						currentHullPainter.paintingData.activeHull = hullIndex;
					}
				}

				if (IsCollumnVisible(Collumn.Delete))
				{
					if (GUI.Button(GetCellRect(row, Collumn.Delete), deleteIcon, EditorStyles.miniButton))
					{
						hullToDelete = hullIndex;
						regenerateOverlay = true;
						repaintSceneView = true;
					}
				}
			}
			GUILayout.EndHorizontal ();
		}

		private void DrawVhacdConfigGUI()
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			bool hasAutoHulls = currentHullPainter.paintingData.HasAutoHulls();

			areVhacdSettingsFoldedOut = EditorGUILayout.Foldout(areVhacdSettingsFoldedOut, new GUIContent("Auto Hull Settings", autoHullSettingsIcon), foldoutStyle);
			if (areVhacdSettingsFoldedOut)
			{
				GUI.enabled = hasAutoHulls;

				currentHullPainter.paintingData.autoHullPreset = (AutoHullPreset)EditorGUILayout.EnumPopup(new GUIContent("Preset"), currentHullPainter.paintingData.autoHullPreset);

				VhacdParameters vhacdParams = GetParameters(currentHullPainter.paintingData, currentHullPainter.paintingData.autoHullPreset);

				GUI.enabled = hasAutoHulls && currentHullPainter.paintingData.autoHullPreset == AutoHullPreset.Custom;

				float concavity = RoundToTwoDecimalPlaces( Mathf.Clamp01(1.0f - vhacdParams.concavity) * 100.0f );
				concavity = EditorGUILayout.Slider(new GUIContent("Concavity", "Lower for more convex, higher for more concave"), concavity, 0f, 100.0f); // invert this. %age
				float newConcavity = 1.0f - (concavity / 100.0f);

				// Resolution / granularity has a huge range (1k to 50 million) and there's serious diminishing returns at the high end
				// (plus it takes a lot longer to process)
				// Remap the resolution into a non-linear 'granularity' where the slider has more low-end values than high end
				// We do this by converting the linear resolution to an exponential granularity [0..1] and then into a %age
				// Converting back is just the reverse: %age -> [0..1] -> resolution

				float granularity = Remap((float)vhacdParams.resolution, 1000u, 50000000u, 0.0f, 1.0f);
				granularity = RoundToTwoDecimalPlaces( ToExponential(granularity) * 100.0f );
				granularity = EditorGUILayout.Slider(new GUIContent("Granularity", "Higher values are more sensitive to fine detail in the input, but also increases time to calculate the result"), granularity, 0f, 100f); // resolution - %age this [10,000 - 50,000,000]
				granularity = FromExponential(granularity / 100.0f);
				uint newResolution = (uint)Remap(granularity, 0.0f, 1.0f, 1000u, 50000000u);
								
				float smoothness = RoundToTwoDecimalPlaces( Remap(vhacdParams.minVolumePerCH, 0.0f, 0.02f, 100.0f, 0.0f) );
				smoothness = EditorGUILayout.Slider(new GUIContent("Smoothness", "Higher values generate higher poly output, lower values are blockier but more efficient"), smoothness, 0, 100);
				float newMinVolumePerCH = Remap(smoothness, 0.0f, 100.0f, 0.02f, 0.0f);

				float symBias = RoundToTwoDecimalPlaces( Remap(vhacdParams.alpha, 0f, 1f, 0f, 100f) );
				symBias = EditorGUILayout.Slider(new GUIContent("Symmetry bias", "Bias the cut planes to symetric axies"), symBias, 0, 100);
				float newAlpha = Remap(symBias, 0f, 100f, 0f, 1f);

				float revBias = RoundToTwoDecimalPlaces( Remap(vhacdParams.beta, 0f, 1f, 0f, 100f) );
				revBias = EditorGUILayout.Slider(new GUIContent("Revolution bias", "Bias the cut planes to revolution axies"), revBias, 0, 100);
				float newBeta = Remap(revBias, 0f, 100f, 0f, 1f);

				uint newMaxConvexHulls = (uint)EditorGUILayout.IntSlider(new GUIContent("Max number of hulls", "The maximum number of hulls that the algorithm will target"), (int)vhacdParams.maxConvexHulls, 0, 1024);
				
				if (vhacdParams.concavity != newConcavity
					|| vhacdParams.resolution != newResolution
					|| vhacdParams.minVolumePerCH!= newMinVolumePerCH
					|| vhacdParams.alpha != newAlpha
					|| vhacdParams.beta != newBeta
					|| vhacdParams.maxConvexHulls != newMaxConvexHulls)
				{

					vhacdParams.concavity = newConcavity;
					vhacdParams.resolution = newResolution;
					vhacdParams.minVolumePerCH = newMinVolumePerCH;
					vhacdParams.alpha = newAlpha;
					vhacdParams.beta = newBeta;
					vhacdParams.maxConvexHulls = newMaxConvexHulls;

					currentHullPainter.paintingData.hasLastVhacdTimings = false;

					EditorUtility.SetDirty(currentHullPainter.paintingData);
				}

				if (GUILayout.Button("Reset to defaults"))
				{
					currentHullPainter.paintingData.vhacdParams = GetParameters(null, AutoHullPreset.Medium);
					currentHullPainter.paintingData.hasLastVhacdTimings = false;
					EditorUtility.SetDirty(currentHullPainter.paintingData);
				}

				GUI.enabled = true;
			}
			DrawUiDivider();
		}

		private static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
		{
			float inRange = inMax - inMin;
			float normalised = (value-inMin) / inRange;

			float outRange = outMax - outMin;
			float result = (normalised * outRange) + outMin;

			return result;
		}

		// To/From exponential conversion
		// The 'power' factor in ToExponential becomes the inverse in FromExponential
		// eg. x^2 and sqrt(x)
		// Larger power factors create a steeper graph (and therefore provide more weight towards the lower numbers)
		private static float ToExponential(float input)
		{
			return Mathf.Pow(input, 1.0f / EXPONENTIAL_SCALE);
		}

		private static float FromExponential(float input)
		{
			return Mathf.Pow(input, EXPONENTIAL_SCALE);
		}

		private static float RoundToTwoDecimalPlaces(float input)
		{
			float output = Mathf.Round(input * 100.0f) / 100.0f;
			return output;
		}

		private static VhacdParameters GetParameters(PaintingData paintingData, AutoHullPreset presetType)
		{
			VhacdParameters vhacdParams = new VhacdParameters();

			switch (presetType)
			{
				case AutoHullPreset.Low:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.01f;
					vhacdParams.resolution = 10000;
					vhacdParams.minVolumePerCH = 0.004f;
					vhacdParams.maxConvexHulls = 256;
					break;
				case AutoHullPreset.Medium:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.002f;
					vhacdParams.resolution = 100000;
					vhacdParams.minVolumePerCH = 0.002f;
					vhacdParams.maxConvexHulls = 512;
					break;
				case AutoHullPreset.High:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.000f;
					vhacdParams.resolution = 5000000;
					vhacdParams.minVolumePerCH = 0.001f;
					vhacdParams.maxConvexHulls = 1024;
					break;
				case AutoHullPreset.Placebo:
					vhacdParams = new VhacdParameters();
					vhacdParams.concavity = 0.000f;
					vhacdParams.resolution = 20000000;
					vhacdParams.minVolumePerCH = 0.000f;
					vhacdParams.maxConvexHulls = 1024;
					break;
				case AutoHullPreset.Custom:
					vhacdParams = paintingData.vhacdParams;
					break;
			}

			return vhacdParams;
		}

		private void DrawSettingsGui()
		{
			areSettingsFoldedOut = EditorGUILayout.Foldout(areSettingsFoldedOut, new GUIContent("Settings", settingsIcon), foldoutStyle);
			if (areSettingsFoldedOut)
			{
				float firstColWidth = 100;
				float lastColWidth = 90;

				float baseWidth = EditorGUIUtility.currentViewWidth - 20; // -20px for window chrome
				float fixedWidth = firstColWidth + lastColWidth + 4;
				float flexibleWidth = baseWidth - fixedWidth;
				float[] collumnWidth =
				{
					firstColWidth,
					flexibleWidth,
					lastColWidth,
				};

				DrawDefaultType(collumnWidth);
				DrawDefaultAsChild(collumnWidth);
				DrawDefaultTrigger(collumnWidth);
				DrawDefaultMaterial(collumnWidth);
				DrawFaceDepth(collumnWidth);
				DrawVisibilityToggles(collumnWidth);
				DrawWireframeSettings(collumnWidth);
				DrawDimmingSettings(collumnWidth);

				// TODO: EditorGUILayout.EnumFlagsField added in 2017.3 - use this for collumn visibility drop down
#if UNITY_2017_3_OR_NEWER
				
#endif
			}
			DrawUiDivider();
		}

		private void DrawCollumnToggle(Collumn colType, string label, float width)
		{
			bool isVisible = IsCollumnVisible(colType);
			bool nowVisible = GUILayout.Toggle(isVisible, label, GUILayout.Width(width));
			if (nowVisible)
			{
				visibleCollumns |= colType;
			}
			else
			{
				visibleCollumns &= ~colType;
			}
		}

		private void DrawDefaultType (float[] collumnWidths)
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label("Default type:", GUILayout.Width(collumnWidths[0]));

				defaultType = (HullType)EditorGUILayout.EnumPopup(defaultType, GUILayout.Width(100));

				GUILayout.Label("", GUILayout.Width(collumnWidths[1]-100));

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])) )
				{
					currentHullPainter.SetAllTypes(defaultType);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultMaterial(float[] collumnWidths)
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Default material:", GUILayout.Width(collumnWidths[0]));

				defaultMaterial = (PhysicMaterial)EditorGUILayout.ObjectField(defaultMaterial, typeof(PhysicMaterial), false, GUILayout.Width(collumnWidths[1]+4));
				
				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					currentHullPainter.SetAllMaterials(defaultMaterial);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultAsChild(float[] collumnWidths)
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Default as child:", GUILayout.Width(collumnWidths[0]));

				if (GUILayout.Button(defaultIsChild ? isChildIcon: nonChildIcon, GUILayout.Width(100)))
				{
					defaultIsChild = !defaultIsChild;
				}

				GUILayout.Label("", GUILayout.Width(collumnWidths[1] - 100));

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					currentHullPainter.SetAllAsChild(defaultIsChild);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDefaultTrigger (float[] collumnWidths)
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label("Default trigger:", GUILayout.Width(collumnWidths[0]));

				if (GUILayout.Button(defaultIsTrigger ? triggerOnIcon : triggerOffIcon, GUILayout.Width(100)))
				{
					defaultIsTrigger = !defaultIsTrigger;
				}

				GUILayout.Label("", GUILayout.Width(collumnWidths[1]-100));

				if (GUILayout.Button("Apply To All", GUILayout.Width(collumnWidths[2])))
				{
					currentHullPainter.SetAllAsTrigger(defaultIsTrigger);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawFaceDepth (float[] collumnWidths)
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label("Face thickness:", GUILayout.Width(collumnWidths[0]));

				currentHullPainter.paintingData.faceThickness = EditorGUILayout.FloatField(currentHullPainter.paintingData.faceThickness, GUILayout.Width(collumnWidths[1]+4));

				float inc = 0.1f;
				if (GUILayout.Button("+", GUILayout.Width((collumnWidths[2]-4)/2)))
				{
					currentHullPainter.paintingData.faceThickness = currentHullPainter.paintingData.faceThickness + inc;
				}
				if (GUILayout.Button("-", GUILayout.Width((collumnWidths[2]-4)/2)))
				{
					currentHullPainter.paintingData.faceThickness = currentHullPainter.paintingData.faceThickness - inc;
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawVisibilityToggles(float[] collumnWidths)
		{
			float toggleWidth = 70.0f;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Show columns:", GUILayout.Width(collumnWidths[0]));
			DrawCollumnToggle(Collumn.Visibility, "Visibility", toggleWidth);
			DrawCollumnToggle(Collumn.Name, "Name", toggleWidth);
			DrawCollumnToggle(Collumn.Colour, "Colour", toggleWidth);
			DrawCollumnToggle(Collumn.Type, "Type", toggleWidth);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("", GUILayout.Width(collumnWidths[0]));
			DrawCollumnToggle(Collumn.Material, "Material", toggleWidth);
			DrawCollumnToggle(Collumn.Inflate, "Inflation", toggleWidth);
			DrawCollumnToggle(Collumn.BoxFitMethod, "Box Fit", toggleWidth);
			DrawCollumnToggle(Collumn.IsChild, "As Child", toggleWidth);
			DrawCollumnToggle(Collumn.Trigger, "Trigger", toggleWidth);
			GUILayout.EndHorizontal();
		}

		private void DrawWireframeSettings(float[] collumnWidths)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Draw wireframe:", GUILayout.Width(collumnWidths[0]));

				wireframeFactor = GUILayout.HorizontalSlider(wireframeFactor, -1.0f, 1.0f, GUILayout.Width(collumnWidths[1]));

				if (GUILayout.Button(showWireframe ? triggerOnIcon : triggerOffIcon, GUILayout.Width(collumnWidths[2])))
				{
					showWireframe = !showWireframe;
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawDimmingSettings(float[] collumnWidths)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Dim other hulls:", GUILayout.Width(collumnWidths[0]));

				GUI.enabled = dimInactiveHulls;
				this.dimFactor = GUILayout.HorizontalSlider(this.dimFactor, 0.0f, 1.0f, GUILayout.Width(collumnWidths[1]));
				GUI.enabled = true;

				if (GUILayout.Button(dimInactiveHulls ? triggerOnIcon : triggerOffIcon, GUILayout.Width(collumnWidths[2])))
				{
					dimInactiveHulls = !dimInactiveHulls;
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawHullWarnings (HullPainter currentHullPainter)
		{
			List<string> warnings = new List<string> ();

			for (int i=0; i<currentHullPainter.paintingData.hulls.Count; i++)
			{
				Hull hull = currentHullPainter.paintingData.hulls[i];
				if (hull.hasColliderError)
				{
					warnings.Add("'"+hull.name+"' generates a collider with "+hull.numColliderFaces+" faces");
				}
			}
			
			if (warnings.Count > 0)
			{
				areErrorsFoldedOut = EditorGUILayout.Foldout(areErrorsFoldedOut, new GUIContent("Warnings", errorIcon), foldoutStyle);
				if (areErrorsFoldedOut)
				{
					foreach (string str in warnings)
					{
						GUILayout.Label(str);
					}

					GUILayout.Label("Unity only allows max 256 faces per hull");
					GUILayout.Space(10);
					GUILayout.Label("Inflation has been enabled to further simplify this hull,");
					GUILayout.Label("adjust the inflation amount to refine this further.");
				}
				DrawUiDivider();
			}
		}

		private void DrawAssetGui()
		{
			areAssetsFoldedOut = EditorGUILayout.Foldout(areAssetsFoldedOut, new GUIContent("Assets", assetsIcon), foldoutStyle);
			if (areAssetsFoldedOut)
			{
				HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();
				
				string paintingPath = AssetDatabase.GetAssetPath(currentHullPainter.paintingData);
				GUILayout.Label("Painting data: "+paintingPath, EditorStyles.centeredGreyMiniLabel);
			
				string hullPath = AssetDatabase.GetAssetPath(currentHullPainter.hullData);
				GUILayout.Label("Hull data: "+hullPath, EditorStyles.centeredGreyMiniLabel);

				if (GUILayout.Button("Disconnect from assets"))
				{
					bool deleteChildren = !EditorUtility.DisplayDialog("Disconnect from assets",
													"Also delete child painter components?\n\n"
													+ "These are not needed but leaving them make it easier to reconnect the painting data in the future.",
													"Leave",    // ok option - returns true
													"Delete");	// cancel option - returns false

					sceneManipulator.DisconnectAssets(deleteChildren);

					currentHullPainter = null;
					repaintSceneView = true;
					regenerateOverlay = true;
				}
			}
		}

#if UNITY_2019_1_OR_NEWER
		public void OnBeforeSceneGUI(SceneView sceneView)
		{
			ProcessSceneEvents();
		}

		public void OnDuringSceneGUI(SceneView sceneView)
		{
			DrawWireframe();
			DrawBrushCursor();
			sceneManipulator.DrawCustomCursor();
		}
#endif
		
		public void OnSceneGUI(SceneView sceneView)
		{
#if !UNITY_2019_1_OR_NEWER
			ProcessSceneEvents();

			DrawWireframe();
			DrawBrushCursor();
			sceneManipulator.DrawCustomCursor();
#endif
		}
		
		private void ProcessSceneEvents()
		{
			if (sceneManipulator.Sync ())
			{
				Repaint();
			}
			
			int controlId = GUIUtility.GetControlID (FocusType.Passive);
			
			if (Event.current.type == EventType.MouseDown && (Event.current.button == 0) && !Event.current.alt)
			{
				// If shift is held then always add, if control then always subtract, otherwise use intelligent pick mode
				PickMode mode = PickMode.Undecided;
				if (Event.current.shift)
					mode = PickMode.Additive;
				else if (Event.current.control)
					mode = PickMode.Subtractive;

				bool eventConsumed = sceneManipulator.DoMouseDown(mode);
				if (eventConsumed)
				{
					activeMouseButton = Event.current.button;
					GUIUtility.hotControl = controlId;
					Event.current.Use();
				}

			}
			else if (Event.current.type == EventType.MouseDrag && Event.current.button == activeMouseButton && !Event.current.alt)
			{
				bool eventConsumed = sceneManipulator.DoMouseDrag();
				if (eventConsumed)
				{
					GUIUtility.hotControl = controlId;
					Event.current.Use();
					Repaint();
				}

			}
			else if (Event.current.type == EventType.MouseUp && Event.current.button == activeMouseButton && !Event.current.alt)
			{
				bool eventConsumed = sceneManipulator.DoMouseUp();
				if (eventConsumed)
				{
					activeMouseButton = -1;
					GUIUtility.hotControl = 0;
					Event.current.Use();
				}
			}
		}

		private void DrawWireframe()
		{
			if (!showWireframe)
				return;

			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();
			if (currentHullPainter != null && currentHullPainter.paintingData != null && currentHullPainter.paintingData.sourceMesh != null && Camera.current != null)
			{
				Mesh srcMesh = currentHullPainter.paintingData.sourceMesh;
				Vector3[] vertices = srcMesh.vertices;
				int[] indices = srcMesh.triangles;

				
				Camera cam = Camera.current;

				if (wireframeFactor < 0.0f)
					Handles.color = new Color(0.0f, 0.0f, 0.0f, -wireframeFactor);
				else
					Handles.color = new Color(1.0f, 1.0f, 1.0f, wireframeFactor);

				Matrix4x4 localToWorld = Matrix4x4.TRS(currentHullPainter.transform.position, currentHullPainter.transform.rotation, currentHullPainter.transform.lossyScale);
				
				Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

				for (int i=0; i<indices.Length; i+=3)
				{
					int i0 = indices[i];
					int i1 = indices[i+1];
					int i2 = indices[i+2];

					Vector3 v0 = vertices[i0];
					Vector3 v1 = vertices[i1];
					Vector3 v2 = vertices[i2];

					v0 = localToWorld.MultiplyPoint(v0);
					v1 = localToWorld.MultiplyPoint(v1);
					v2 = localToWorld.MultiplyPoint(v2);

					Vector3 center = (v0 + v1 + v2) / 3.0f;

					Vector3 e0 = (v1 - v0).normalized;
					Vector3 e1 = (v2 - v1).normalized;
					Vector3 normal = Vector3.Cross(e0, e1).normalized;
					
					Vector3 viewDir = (center - cam.transform.position).normalized;
					float dot = Vector3.Dot(normal, viewDir);
					if (dot < 0.0f)
					{
						Handles.DrawLine(v0, v1);
						Handles.DrawLine(v1, v2);
						Handles.DrawLine(v2, v0);
					}
				}
			}
		}

		private void DrawBrushCursor()
		{
			if (Event.current.type == EventType.Repaint)
			{
				if (sceneManipulator.GetCurrentTool() == ToolSelection.TrianglePainting)
				{
					Handles.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

					int pickRadius = sceneManipulator.GetBrushPixelSize();

					Ray centerRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					Ray rightRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition + new Vector2(pickRadius, 0.0f));
					Ray upRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition + new Vector2(0.0f, pickRadius));

					Vector3 centerPos = centerRay.origin + centerRay.direction;
					Vector3 upPos = upRay.origin + upRay.direction;
					Vector3 rightPos = rightRay.origin + rightRay.direction;

					Vector3 upVec = upPos - centerPos;
					Vector3 rightVec = rightPos - centerPos;

					List<Vector3> points = new List<Vector3>();

					int numSegments = 20;

					for (int i = 0; i < numSegments; i++)
					{
						float angle0 = (float)i / (float)numSegments * Mathf.PI * 2.0f;
						float angle1 = (float)(i + 1) / (float)numSegments * Mathf.PI * 2.0f;

						Vector3 p0 = centerPos + (rightVec * Mathf.Cos(angle0)) + (upVec * Mathf.Sin(angle0));
						Vector3 p1 = centerPos + (rightVec * Mathf.Cos(angle1)) + (upVec * Mathf.Sin(angle1));

						points.Add(p0);
						points.Add(p1);
					}

					Handles.DrawLines(points.ToArray());
				}
			}
		}

		private void GenerateColliders()
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();
			if (currentHullPainter == null)
				return;

			// TODO: Don't trigger the generate routine if it's already running
			// ..

			VhacdParameters parameters = GetParameters(currentHullPainter.paintingData, currentHullPainter.paintingData.autoHullPreset);
			
			EditorCoroutines.Execute(GenerateCollidersRoutine(currentHullPainter, parameters));
		}

		private IEnumerator GenerateCollidersRoutine(HullPainter currentHullPainter, VhacdParameters parameters)
		{
			yield return null;

			Mesh[] autoHulls = null;

			DateTime startTime = DateTime.Now;
			float progress = 0.0f;

			// Do we need to run VHACD to generate auto hulls?
			if (currentHullPainter.paintingData.HasAutoHulls())
			{
				// Run VHACD in a background thread

				VhacdTask task = new VhacdTask();
				task.Init(currentHullPainter.paintingData.sourceMesh, parameters);
				task.Run();

				float expectedDuration = currentHullPainter.paintingData.hasLastVhacdTimings ? (currentHullPainter.paintingData.lastVhacdDurationSecs+30) : 45.0f;

				do
				{
					// Idea: Keep track of how long this took last run (for this mesh) and make the progress bar animate over the same time

					float duration = (float)(DateTime.Now - startTime).TotalSeconds;
					progress = TimeProgression(duration, expectedDuration);

					EditorUtility.DisplayProgressBar("Generating convex hulls",
													string.Format("Calculating... {0} seconds so far... {1}%", duration.ToString("0.0"), (progress*100).ToString("0.0")),
													progress);
					yield return null;
				}
				while (!task.IsFinished());

				task.Finalise();

				// TODO: Keep track of auto hulls in PaintingData? (or new AutoHullData?)
				// Also keep track of parameters used and time taken
				// Then avoid recalculating this if we've not changed mesh or parameters and just re-use it

				autoHulls = task.OutputHulls;
			}

			Undo.SetCurrentGroupName("Generate Colliders");
			Undo.RegisterCompleteObjectUndo (currentHullPainter.gameObject, "Generate");

			// Fetch the data assets

			PaintingData paintingData = currentHullPainter.paintingData;
			HullData hullData = currentHullPainter.hullData;

			string hullAssetPath = AssetDatabase.GetAssetPath (hullData);
			
			// Create / update the hull meshes

			foreach (Hull hull in paintingData.hulls)
			{
				paintingData.GenerateCollisionMesh(hull, sceneManipulator.GetTargetVertices(), sceneManipulator.GetTargetTriangles(), autoHulls);
			}

			// Sync the in-memory hull meshes with the asset meshes in hullAssetPath

			List<Mesh> existingMeshes = GetAllMeshesInAsset (hullAssetPath);

			foreach (Mesh existing in existingMeshes)
			{
				if (!paintingData.ContainsMesh(existing))
				{
					GameObject.DestroyImmediate(existing, true);
				}
			}

			foreach (Hull hull in paintingData.hulls)
			{
				if (hull.collisionMesh != null)
				{
					if (!existingMeshes.Contains(hull.collisionMesh))
					{
						AssetDatabase.AddObjectToAsset(hull.collisionMesh, hullAssetPath);
					}
				}
				if (hull.faceCollisionMesh != null)
				{
					if (!existingMeshes.Contains(hull.faceCollisionMesh))
					{
						AssetDatabase.AddObjectToAsset(hull.faceCollisionMesh, hullAssetPath);
					}
				}

				if (hull.autoMeshes != null)
				{
					foreach (Mesh auto in hull.autoMeshes)
					{
						if (!existingMeshes.Contains(auto))
						{
							AssetDatabase.AddObjectToAsset(auto, hullAssetPath);
						}
					}
				}
			}

			
			EditorUtility.SetDirty (hullData);

			AssetDatabase.SaveAssets ();

			// Add collider components to the target object

			currentHullPainter.CreateColliderComponents (autoHulls);

			EditorUtility.SetDirty(currentHullPainter);

			// Zip the progress bar up to 100% to finish it, otherwise it disappears before reaching the end and that looks broken

			int numSteps = progress < 50 ? 10 : 30; // If we've not made it to 50%, do a quick zip, otherwise do a slightly longer one
			float inc = (1.0f - progress) / numSteps;
			for (int i=0; i<numSteps; i++)
			{
				progress += inc;
				float duration = (float)(DateTime.Now - startTime).TotalSeconds;
				EditorUtility.DisplayProgressBar("Generating convex hulls",
													string.Format("Calculating... {0} seconds so far... {1}%", duration.ToString("0.0"), (progress * 100).ToString("0.0")),
													progress);
				yield return null;
			}

			// Output overal stats to the console (todo: move this to an output section in the window)

			float totalDurationSecs = (float)(DateTime.Now - startTime).TotalSeconds;
			int numColliders = paintingData.TotalOutputColliders;
			Debug.Log(string.Format("Collider Creator created {0} colliders in {1} seconds", numColliders, totalDurationSecs.ToString("0.00")));
			
			// Finished! hide the progress bar
			UnityEditor.EditorUtility.ClearProgressBar();
		}

		private static float TimeProgression(float elapsedTime, float maxTime)
		{
			float normalizedTime = elapsedTime / maxTime;

			float result = -((-normalizedTime) / (normalizedTime + (1.0f/2.0f)));	// 1/4 - reaches 80% of maxTime in maxTime seconds
																					// 1/2 - reaches 80% of maxTime in maxTime*2 seconds
			return result;
		}

		private static float AsymtopicProgression(float inputProgress, float maxProgression, float rate)
		{
			/* Asymtopic progression curve
			 *               b(-x)
			 * f(x) =  -1 * ------- 
			 *              (x + a)
			 * 
			 * Where:
			 *	x - input progress (time)
			 *	b - value to approach (but never reach)
			 *	a - rate of approach (higher values quickly reach near to max value before slowing down, lower values are smoother
			 *	
			 */
			float result = -((maxProgression * (-inputProgress)) / (inputProgress + rate));
			return result;
		}

		private void AddHull()
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			if (currentHullPainter != null)
			{
				Undo.RecordObject (currentHullPainter.paintingData, "Add Hull");
				currentHullPainter.paintingData.AddHull(defaultType, defaultMaterial, defaultIsChild, defaultIsTrigger);

				EditorUtility.SetDirty (currentHullPainter.paintingData);
			}
		}

		private void SetAllHullsVisible(bool visible)
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				for (int i=0; i<currentHullPainter.paintingData.hulls.Count; i++)
				{
					currentHullPainter.paintingData.hulls[i].isVisible = visible;
				}
			}

			regenerateOverlay = true;
		}

		private void StopPainting()
		{
			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();

			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				currentHullPainter.paintingData.activeHull = -1;
			}
		}

		private void DeleteColliders()
		{
			Undo.SetCurrentGroupName ("Delete Colliders");

			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();
			currentHullPainter.RemoveAllColliders ();
		}

		private void DeleteGenerated()
		{
			Undo.SetCurrentGroupName("Delete Generated Objects");

			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();
			currentHullPainter.RemoveAllGenerated ();
		}

		private void DeleteHulls ()
		{
			Undo.SetCurrentGroupName("Delete All Hulls");

			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter ();
			if (currentHullPainter != null && currentHullPainter.hullData != null)
			{
				currentHullPainter.paintingData.RemoveAllHulls ();
				repaintSceneView = true;
			}
		}

		private bool AreAllHullsVisible()
		{
			bool allVisible = true;

			HullPainter currentHullPainter = sceneManipulator.GetCurrentHullPainter();
			if (currentHullPainter != null && currentHullPainter.paintingData != null)
			{
				for (int i = 0; i < currentHullPainter.paintingData.hulls.Count; i++)
				{
					if (!currentHullPainter.paintingData.hulls[i].isVisible)
					{
						allVisible = false;
						break;
					}
				}
			}

			return allVisible;
		}

		private bool IsCollumnVisible(Collumn col)
		{
			return (visibleCollumns & col) > 0;
		}

		private List<Mesh> GetAllMeshesInAsset(string assetPath)
		{
			List<Mesh> meshes = new List<Mesh> ();

			foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(assetPath))
			{
				if (o is Mesh)
				{
					meshes.Add((Mesh)o);
				}
			}

			return meshes;
		}

		public bool ShouldDimInactiveHulls()
		{
			return dimInactiveHulls;
		}

		public float GetInactiveHullDimFactor()
		{
			return dimFactor;
		}

		public static string FindInstallPath()
		{
			string installPath = defaultInstallPath;
			
			string[] foundIds = AssetDatabase.FindAssets("PhysicsCreatorInstallRoot t:PhysicsCreatorInstallRoot");
			if (foundIds.Length > 0)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath (foundIds [0]);
				int lastSlashPos = assetPath.LastIndexOf("/");
				if (lastSlashPos != -1)
				{
					string newInstallPath = assetPath.Substring(0, lastSlashPos+1);
					installPath = newInstallPath;
				}
			}

			return installPath;
		}

		private void DrawUiDivider()
		{
			DrawUiDivider(dividerColour);
		}

		public static void DrawUiDivider(Color color, int thickness = 1, int padding = 10)
		{
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
			r.height = thickness;
			r.y += padding / 2;
			r.x -= 2;
			r.width += 6;
			EditorGUI.DrawRect(r, color);
		}

	}

} // namespace Technie.PhysicsCreator
