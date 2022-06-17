using UnityEngine;

namespace Technie.PhysicsCreator
{
	public class AxisAlignedBoxFitter
	{
		public void Fit(Hull hull, Vector3[] meshVertices, int[] meshIndices)
		{
			Vector3[] selectedVertices = FaceAlignmentBoxFitter.GetSelectedVertices(hull, meshVertices, meshIndices);
			ConstructionPlane basePlane = new ConstructionPlane(Vector3.zero, Vector3.up, Vector3.right);
			RotatedBox box = RotatedBoxFitter.FindTightestBox(basePlane, selectedVertices);
			RotatedBoxFitter.ApplyToHull(box, hull);
		}
	}
	
} // namespace Technie.PhysicsCreator