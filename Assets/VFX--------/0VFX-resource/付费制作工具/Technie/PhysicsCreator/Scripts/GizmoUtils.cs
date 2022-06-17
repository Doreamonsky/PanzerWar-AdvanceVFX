using System;
using System.Reflection;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif
 
namespace Technie.PhysicsCreator
{
	 public class GizmoUtils
	 {
		public static void ToggleGizmos(bool gizmosOn)
		{
	#if UNITY_EDITOR
			int val = gizmosOn ? 1 : 0;
			Assembly asm = Assembly.GetAssembly(typeof(Editor));
			Type type = asm.GetType("UnityEditor.AnnotationUtility");
			if (type != null)
			{
				MethodInfo getAnnotations = type.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo setGizmoEnabled = type.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo setIconEnabled = type.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
				var annotations = getAnnotations.Invoke(null, null);
				foreach (object annotation in (IEnumerable)annotations)
				{
					Type annotationType = annotation.GetType();
					FieldInfo classIdField = annotationType.GetField("classID", BindingFlags.Public | BindingFlags.Instance);
					FieldInfo scriptClassField = annotationType.GetField("scriptClass", BindingFlags.Public | BindingFlags.Instance);
					if (classIdField != null && scriptClassField != null)
					{
						int classId = (int)classIdField.GetValue(annotation);
						string scriptClass = (string)scriptClassField.GetValue(annotation);

						if (scriptClass == "HullPainter")
						{
							int numGizmoParams = setGizmoEnabled.GetParameters().Length;
							if (numGizmoParams == 3)
							{
								setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val });
							}
							else if (numGizmoParams == 4)
							{
								setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val, true });
							}

							int numIconParams = setIconEnabled.GetParameters().Length;
							if (numIconParams == 3)
							{
								setIconEnabled.Invoke(null, new object[] { classId, scriptClass, val });
							}
						}
					}
				}
			}
	#endif
		}
	}
 } // namespace Technie.PhysicsCreator
 