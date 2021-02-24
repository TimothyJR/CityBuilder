using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Sidewalk))]
public class SidewalkEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		// Button to generate the mesh again
		if(GUILayout.Button("Regenerate Mesh"))
		{
			Sidewalk sidewalk = target as Sidewalk;
			sidewalk.CalculateVertices();
			sidewalk.CreateMesh();
		}
	}
}