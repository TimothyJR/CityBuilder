using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CityGenerator))]
public class CityGeneratorEditor : Editor
{
	/// <summary>
	/// The current selected intersection
	/// </summary>
	private Intersection selectedNode = null;

	/// <summary>
	/// Whether the modifier key is pressed or not
	/// </summary>
	private bool modifierKey = false;

	/// <summary>
	/// The plane on the xz axis
	/// </summary>
	private Plane horizontalPlane = new Plane(Vector3.up, Vector3.zero);

	/// <summary>
	/// The city asset that is being used for generation
	/// </summary>
	private CityInfo city = null;

	/// <summary>
	/// The city asset of last frame
	/// Used to check if the city asset changes
	/// </summary>
	private CityInfo previousCity = null;

	/// <summary>
	/// Holds control IDs for the intersections to find the selected nodes
	/// </summary>
	private Dictionary<int, Intersection> selectionLookUp = new Dictionary<int, Intersection>();

	#region Inspector

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		city = (target as CityGenerator).City;

		if (city != null)
		{
			if (city != previousCity)
			{
				Init();
			}

			// Get all material fields
			city.RoadMaterial = EditorGUILayout.ObjectField("Road Material", city.RoadMaterial, typeof(Material), false) as Material;
			city.SidewalkMaterial = EditorGUILayout.ObjectField("General Sidewalk Material", city.SidewalkMaterial, typeof(Material), false) as Material;
			city.InnerBlockMaterial = EditorGUILayout.ObjectField("Sidewalk Fill Material", city.InnerBlockMaterial, typeof(Material), false) as Material;

			// Create our building list
			SerializedObject cityObject = new SerializedObject(city);
			SerializedProperty buildingList = cityObject.FindProperty("buildings");
			EditorGUILayout.PropertyField(buildingList);
			cityObject.ApplyModifiedProperties();

			// Button to generate the city
			if (GUILayout.Button("Generate City"))
			{
				GenerateIntersections();
				GenerateRoads();
				GenerateBlock();
			}

			// Inspector for the Intersectiosn
			if (selectedNode != null)
			{
				GUILayout.Label($"Selected Intersection Tools:");
				StartIndent(10.0f);
				{
					selectedNode.Position = EditorGUILayout.Vector3Field("Position", selectedNode.Position);
					if (GUILayout.Button("Merge Nearby"))
					{
						MergeIntersectionByDistance(selectedNode);
					}

					if (GUILayout.Button("Delete Intersection"))
					{
						if (city.Nodes.Count > 1)
						{
							DeleteIntersection(selectedNode);
						}
						else
						{
							Debug.Log("Cannot delete the final intersection of this city");
						}
					}
				}
				EndIndent();
			}

			// Prevent a null ref if the selected node got deleted in the above code
			// TODO: Add tabs to code and remove this if check and put it under a "Road" Tab
			if(selectedNode != null)
			{
				// Inspector for Roads
				GUILayout.Label("Road Tools");
				StartIndent(10.0f);
				{
					for (int i = 0; i < selectedNode.Roads.Count; i++)
					{
						GUILayout.Label($"Road {i}:");
						StartIndent(30.0f);
						{
							selectedNode.Roads[i].RoadWidth = EditorGUILayout.FloatField("Width:", selectedNode.Roads[i].RoadWidth);
							selectedNode.Roads[i].SetRoadOffset(EditorGUILayout.FloatField("Offset:", selectedNode.Roads[i].GetRoadOffset(selectedNode)), selectedNode);
							GUILayout.BeginHorizontal();
							{

								if (GUILayout.Button("Split"))
								{
									selectedNode.SplitRoad(selectedNode.Roads[i], city);
								}

								if (GUILayout.Button("Delete"))
								{
									selectedNode.RemoveConnection(selectedNode.Roads[i]);
								}
							}
							GUILayout.EndHorizontal();
						}
						EndIndent();
					}
				}
				EndIndent();
			}
		}

		previousCity = city;
	}

	/// <summary>
	/// Creates an inspector indent
	/// </summary>
	/// <param name="space"></param>
	private void StartIndent(float space)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Space(space);
		GUILayout.BeginVertical();
	}

	/// <summary>
	/// Ends an inspector indent
	/// </summary>
	private void EndIndent()
	{
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
	}

	#endregion

	#region Scene View

	public void OnSceneGUI()
	{
		if (city == null)
		{
			city = (target as CityGenerator).City;
		}

		if (city != null && city.Nodes != null && city.Nodes.Count > 0)
		{
			DrawCityGraph();
		}

		// Get event info
		Event e = Event.current;

		switch (e.type)
		{
			case EventType.KeyDown:
				if (e.keyCode == KeyCode.LeftControl)
				{
					modifierKey = true;
				}
				else if (e.keyCode == KeyCode.M && modifierKey)
				{
					// Ctrl + M for merge
					MergeIntersectionByDistance(selectedNode);
				}
				break;
			case EventType.KeyUp:
				if (e.keyCode == KeyCode.LeftControl)
				{
					modifierKey = false;
				}
				break;
			case EventType.MouseDown:
				// Ctrl + Click causes an intersection to be created from the selected node
				if (modifierKey && selectedNode != null)
				{
					Vector3 mousePosition = Event.current.mousePosition;
					Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

					float distance = 0.0f;
					if (horizontalPlane.Raycast(ray, out distance))
					{
						selectedNode = selectedNode.CreateNewIntersection(ray.GetPoint(distance), city);
					}
					else
					{
						Debug.LogWarning("Could not create new intersection since point was attempting to be made in the sky.");
					}
				}
				break;
			default:
				break;
		}

		Selection.activeGameObject = (target as CityGenerator).gameObject;
	}

	/// <summary>
	/// Initialize the city if there is nothing
	/// </summary>
	private void Init()
	{
		if (city.Nodes == null)
		{
			city.Nodes = new List<Intersection>();
		}

		if (city.Nodes.Count == 0)
		{
			Intersection intersection = Intersection.CreateIntersection(Vector3.zero, city);
			AssetDatabase.SaveAssets();
			selectedNode = intersection;
			city.Nodes.Add(intersection);
		}
	}

	/// <summary>
	/// Draws the city preview in the scene view
	/// </summary>
	private void DrawCityGraph()
	{
		selectionLookUp.Clear();

		for (int i = 0; i < city.Nodes.Count; i++)
		{
			// Draw selection as green
			if (city.Nodes[i] == selectedNode)
			{
				Handles.color = Color.green;
			}
			else
			{
				Handles.color = Color.white;
			}

			Handles.DrawWireCube(city.Nodes[i].Position, new Vector3(3.0f, 3.0f, 3.0f));

			// Draw the movement handle
			float handleSize = HandleUtility.GetHandleSize(city.Nodes[i].Position) * 0.25f;

			city.Nodes[i].Position = Handles.FreeMoveHandle(city.Nodes[i].Position, Quaternion.identity, handleSize, Vector3.one * 0.5f,
				(controlID, position, rotation, size, eventType) =>
				{
					selectionLookUp.Add(controlID, city.Nodes[i]);
					Handles.SphereHandleCap(controlID, position, rotation, size, eventType);
				});

			// TODO: Movement should be projected towards the xz plane instead of just forcing down to prevent weird movement when not using topdown view
			city.Nodes[i].Position = new Vector3(city.Nodes[i].Position.x, 0.0f, city.Nodes[i].Position.z);

			// Draw the right side of the road from the intersection
			// TODO: Add list of all roads to city info to optimize this.
			for (int j = 0; j < city.Nodes[i].Roads.Count; j++)
			{
				if (city.Nodes[i] == selectedNode)
				{
					Handles.color = Color.green;
					DrawRoad(city.Nodes[i].Position, city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i]).Position, city.Nodes[i].Roads[j].RoadWidth, city.Nodes[i].Roads[j].GetRoadOffset(city.Nodes[i]));
					Handles.Label((city.Nodes[i].Position + city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i]).Position) / 2, j.ToString());
				}
				else if (city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i]) == selectedNode)
				{
					int a = j;
					Handles.color = Color.green;
					DrawRoad(city.Nodes[i].Position, city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i]).Position, city.Nodes[i].Roads[j].RoadWidth, city.Nodes[i].Roads[j].GetRoadOffset(city.Nodes[i]));
				}
				else
				{
					Handles.color = Color.white;
					DrawRoad(city.Nodes[i].Position, city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i]).Position, city.Nodes[i].Roads[j].RoadWidth, city.Nodes[i].Roads[j].GetRoadOffset(city.Nodes[i]));
				}
			}
		}

		// Find if any intersections have been selected
		Intersection selection = null;
		if (selectionLookUp.TryGetValue(GUIUtility.hotControl, out selection))
		{
			if (selectedNode != selection)
			{
				selectedNode = selection;
				Repaint();
			}
		}
	}

	/// <summary>
	/// Draws a line from start to end with a right offset of the road width
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <param name="roadWidth"></param>
	private void DrawRoad(Vector3 start, Vector3 end, float roadWidth, float offset)
	{
		Vector3 offsetDirection = start - end;
		offsetDirection = new Vector3(-offsetDirection.z, 0.0f, offsetDirection.x).normalized * (roadWidth / 2 + offset);
		Handles.DrawLine(start + offsetDirection, end + offsetDirection);
	}

	#endregion

	/// <summary>
	/// Checks for distance between intersections and if they are within threshold, merge them
	/// TODO: Spacial Partition points for faster checking if speed becomes an issue
	/// </summary>
	/// <param name="intersection"></param>
	private void MergeIntersectionByDistance(Intersection intersection)
	{
		for (int i = 0; i < city.Nodes.Count; i++)
		{
			if (city.Nodes[i] != intersection)
			{
				if (Vector3.Distance(city.Nodes[i].Position, intersection.Position) < 3.0f)
				{
					city.Nodes[i].MergeIntersection(intersection, city);
					SceneView.RepaintAll();
				}
			}
		}
	}

	/// <summary>
	/// Removes connections when deleting an intersection
	/// </summary>
	/// <param name="toDelete"></param>
	private void DeleteIntersection(Intersection toDelete)
	{
		toDelete.DeleteAllConnections();

		city.Nodes.Remove(toDelete);

		if (selectedNode == toDelete)
		{
			selectedNode = null;
		}

		DestroyImmediate(toDelete, true);

		SceneView.RepaintAll();
	}

	#region Final Generation

	/// <summary>
	/// Finds all the points for the intersection mesh and generates it
	/// </summary>
	private void GenerateIntersections()
	{
		GameObject parentObject = new GameObject();
		parentObject.name = "Intersections";

		PriorityQueue<RoadSegment> roads = new PriorityQueue<RoadSegment>();
		for (int i = 0; i < city.Nodes.Count; i++)
		{
			// If there are more than two roads, a mesh is guaranteed to be generated
			if (city.Nodes[i].Roads.Count > 2)
			{
				// Organize all roads by angle from the right vector
				// Starts clockwise from straight left
				for (int j = 0; j < city.Nodes[i].Roads.Count; j++)
				{
					// Find the vector from the intersection following along the road
					Vector3 angleToCheck = city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i]).Position - city.Nodes[i].Position;
					roads.Enqueue(city.Nodes[i].Roads[j], Vector3.SignedAngle(Vector3.right, angleToCheck, Vector3.up));
				}

				// Find the points for the intersection
				// Reorganize the roads so we don't need to create priority queues for the sidewalks later
				List<Vector3> vertices = new List<Vector3>();
				RoadSegment first = roads.Dequeue();
				RoadSegment left = null;
				RoadSegment right = first;
				city.Nodes[i].Roads[0] = right;
				int index = 1;

				while (roads.Count > 0)
				{
					left = right;
					right = roads.Dequeue();
					GetRoadPoint(right, left, city.Nodes[i], vertices);
					city.Nodes[i].Roads[index] = right;
					index++;
				}

				// Acount for the final road segment
				GetRoadPoint(first, right, city.Nodes[i], vertices);

				// Add our center point
				vertices.Add(Vector3.zero);

				CreateIntersectionMeshObject(vertices, city.Nodes[i].Position, parentObject);
			}
			else if (city.Nodes[i].Roads.Count > 1)
			{
				// Two roads, find their intersections and add vertices to the segments
				List<Vector3> vertices = new List<Vector3>();
				RoadSegment first = city.Nodes[i].Roads[0];
				RoadSegment second = city.Nodes[i].Roads[1];
				GetRoadPoint(first, second, city.Nodes[i], vertices);
				GetRoadPoint(second, first, city.Nodes[i], vertices);

				// If there are more than 2 vertices, we have to create a mesh
				if (vertices.Count > 2)
				{
					vertices.Add(Vector3.zero);
					CreateIntersectionMeshObject(vertices, city.Nodes[i].Position, parentObject);
				}
			}
			else if (city.Nodes[i].Roads.Count > 0)
			{
				// One road, add vertices to the segments that end at the intersection
				GetRoadPoint(city.Nodes[i].Roads[0], city.Nodes[i], true);
				GetRoadPoint(city.Nodes[i].Roads[0], city.Nodes[i], false);
			}
		}
	}

	/// <summary>
	/// Creates the mesh object in the world
	/// </summary>
	/// <param name="vertices"></param>
	/// <param name="position"></param>
	/// <param name="parentObject"></param>
	private void CreateIntersectionMeshObject(List<Vector3> vertices, Vector3 position, GameObject parentObject)
	{
		int[] triangles = new int[(vertices.Count - 1) * 3];

		// The triangles will form triangle fan, so the center vertex will always be the second vertex in a triangle
		for (int j = 0; j < vertices.Count - 1; j++)
		{
			triangles[j * 3] = (j + 1) % (vertices.Count - 1);
			triangles[j * 3 + 1] = vertices.Count - 1;
			triangles[j * 3 + 2] = j;
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles;

		// Create our mesh object in the world
		GameObject intersection = new GameObject();
		MeshRenderer renderer = intersection.AddComponent<MeshRenderer>();
		MeshFilter filter = intersection.AddComponent<MeshFilter>();
		MeshCollider collider = intersection.AddComponent<MeshCollider>();
		intersection.transform.position = position;
		filter.mesh = mesh;
		collider.sharedMesh = mesh;
		renderer.material = city.RoadMaterial;
		intersection.transform.parent = parentObject.transform;
	}

	/// <summary>
	/// All vertices are set in the GenerateIntersection function
	/// Offset vertices based on distance between intersections
	/// Then create meshes using those vertices
	/// </summary>
	private void GenerateRoads()
	{
		GameObject parentObject = new GameObject();
		parentObject.name = "Roads";

		HashSet<Intersection> finished = new HashSet<Intersection>();

		int[] triangles = { 0, 1, 2, 0, 2, 3 };

		// TODO: Add list of all roads to city info to optimize this
		for (int i = 0; i < city.Nodes.Count; i++)
		{
			finished.Add(city.Nodes[i]);
			for (int j = 0; j < city.Nodes[i].Roads.Count; j++)
			{
				// Only create the road if it wasn't created from another intersection
				if (!finished.Contains(city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i])))
				{
					// Offset vertices
					city.Nodes[i].Roads[j].OffsetVertices();

					Mesh roadMesh = new Mesh();
					roadMesh.vertices = city.Nodes[i].Roads[j].Vertices;
					roadMesh.triangles = triangles;

					// Create our mesh object in the world
					GameObject road = new GameObject();
					MeshRenderer roadRenderer = road.AddComponent<MeshRenderer>();
					MeshFilter roadFilter = road.AddComponent<MeshFilter>();
					MeshCollider collider = road.AddComponent<MeshCollider>();

					// Set the position between the two intersections
					road.transform.position = (city.Nodes[i].Position + city.Nodes[i].Roads[j].GetOtherSide(city.Nodes[i]).Position) / 2;
					roadFilter.mesh = roadMesh;
					collider.sharedMesh = roadMesh;
					roadRenderer.material = city.RoadMaterial;
					road.transform.parent = parentObject.transform;
				}
			}
		}
	}

	/// <summary>
	/// Goes road by road to create sidewalks
	/// Once a side of the road (designated by a road and the intersection it comes from) is used, don't try to use it again
	/// </summary>
	private void GenerateBlock()
	{
		GameObject parentObject = new GameObject();
		parentObject.name = "Block";

		HashSet<SidewalkCheckHelper> usedRoads = new HashSet<SidewalkCheckHelper>();
		List<Sidewalk> sidewalks = new List<Sidewalk>();
		SidewalkCheckHelper check = new SidewalkCheckHelper();

		// TODO: Add list of all roads to city info to optimize this
		for (int i = 0; i < city.Nodes.Count; i++)
		{
			for(int j = 0; j < city.Nodes[i].Roads.Count; j++)
			{
				check.Intersection = city.Nodes[i];
				check.Road = city.Nodes[i].Roads[j];

				if(!usedRoads.Contains(check))
				{
					usedRoads.Add(check);
					sidewalks.Add(CreateSidewalk(city.Nodes[i], city.Nodes[i].Roads[j], usedRoads, parentObject));
				}
			}
		}

		// Create buildings
		// Use collisions to make sure buildings don't collide with roads
		// TODO: Add list of all roads to city info to optimize this
		GameObject buildingObject = new GameObject();
		buildingObject.name = "Buildings";
		buildingObject.transform.parent = parentObject.transform;

		HashSet<RoadSegment> roads = new HashSet<RoadSegment>();
		for(int i = 0; i < city.Nodes.Count; i++)
		{
			for(int j = 0; j < city.Nodes[i].Roads.Count; j++)
			{
				if (!roads.Contains(city.Nodes[i].Roads[j]))
				{
					roads.Add(city.Nodes[i].Roads[j]);
					CreateBuildings(city.Nodes[i].Roads[j], buildingObject);
				}
			}
		}

		// After buildings are created, create the infill mesh
		for(int i = 0; i < sidewalks.Count; i++)
		{
			sidewalks[i].CreateFillMesh();
		}
	}

	/// <summary>
	/// Gets all the points that will form a sidewalk
	/// </summary>
	/// <param name="startIntersection"></param>
	/// <param name="roadSegment"></param>
	/// <param name="checks"></param>
	/// <param name="parent"></param>
	private Sidewalk CreateSidewalk(Intersection startIntersection, RoadSegment roadSegment, HashSet<SidewalkCheckHelper> checks, GameObject parent)
	{
		Intersection current = startIntersection;
		Intersection next = roadSegment.GetOtherSide(startIntersection);
		RoadSegment nextSegment = null;

		GameObject go = new GameObject();
		Sidewalk sidewalk = go.AddComponent<Sidewalk>();
		sidewalk.SidewalkMaterial = city.SidewalkMaterial;
		sidewalk.InnerMaterial = city.InnerBlockMaterial;
		sidewalk.OriginalVertices.Add(roadSegment.GetVertexWorldPosition(startIntersection, false));
		SidewalkCheckHelper check = new SidewalkCheckHelper();

		while (next != startIntersection)
		{
			int nextIndex = (next.Roads.IndexOf(roadSegment) + 1) % (next.Roads.Count);
			nextSegment = next.Roads[nextIndex];

			check.Intersection = next;
			check.Road = nextSegment;

			checks.Add(check);

			if(next.Roads.Count == 1)
			{
				sidewalk.OriginalVertices.Add(nextSegment.GetVertexWorldPosition(next, true));
				sidewalk.OriginalVertices.Add(nextSegment.GetVertexWorldPosition(next, false));
			}
			else if (!(Mathf.Approximately(roadSegment.GetVertexWorldPosition(next, true).x, nextSegment.GetVertexWorldPosition(next, false).x) &&
						Mathf.Approximately(roadSegment.GetVertexWorldPosition(next, true).z, nextSegment.GetVertexWorldPosition(next, false).z)))
			{
				sidewalk.OriginalVertices.Add(roadSegment.GetVertexWorldPosition(next, true));
				sidewalk.OriginalVertices.Add(nextSegment.GetVertexWorldPosition(next, false));
			}
			else
			{
				sidewalk.OriginalVertices.Add(nextSegment.GetVertexWorldPosition(next, false));
			}

			current = next;
			next = nextSegment.GetOtherSide(next);
			roadSegment = nextSegment;
		}

		if(nextSegment != null &&
			!(Mathf.Approximately(sidewalk.OriginalVertices[0].x, nextSegment.GetVertexWorldPosition(next, true).x) &&
			Mathf.Approximately(sidewalk.OriginalVertices[0].z, nextSegment.GetVertexWorldPosition(next, true).z)))
		{
			sidewalk.OriginalVertices.Add(nextSegment.GetVertexWorldPosition(next, true));
		}

		Vector3 sum = Vector3.zero;

		for(int i = 0; i < sidewalk.OriginalVertices.Count; i++)
		{
			sum += sidewalk.OriginalVertices[i];
		}

		go.transform.position = sum / sidewalk.OriginalVertices.Count;
		sidewalk.FixMeshOffset();
		sidewalk.FindDirection();
		sidewalk.CalculateVertices();
		sidewalk.CreateMesh();
		go.transform.parent = parent.transform;

		return sidewalk;
	}

	/// <summary>
	/// Creates the buildings along each side of the road
	/// </summary>
	/// <param name="road"></param>
	/// <param name="parent"></param>
	private void CreateBuildings(RoadSegment road, GameObject parent)
	{
		// Don't do anything if there is nothing to spawn
		if (city.Buildings.Count > 0)
		{
			Vector3 direction = road.ConnectionPointA.Position - road.ConnectionPointB.Position;
			Vector3 perpendicular = new Vector3(direction.z, 0.0f, -direction.x).normalized;

			float finalDistance = direction.magnitude;

			SpawnBuildingsAlongRoad(direction.magnitude, road.ConnectionPointB.Position, perpendicular, direction.normalized, road.GetRoadEdgeOffset(road.ConnectionPointB), parent);
			SpawnBuildingsAlongRoad(direction.magnitude, road.ConnectionPointB.Position, -perpendicular, direction.normalized, road.GetRoadEdgeOffset(road.ConnectionPointA), parent);
		}
		else
		{
			Debug.Log("There are no buildings to spawn in the building list.");
		}
	}

	/// <summary>
	/// Checks to make sure buildings are not colliding with anything so that they have room to spawn
	/// </summary>
	/// <param name="finalDistance"></param>
	/// <param name="startingPosition"></param>
	/// <param name="perpendicular"></param>
	/// <param name="direction"></param>
	/// <param name="roadSize"></param>
	/// <param name="parent"></param>
	private void SpawnBuildingsAlongRoad(float finalDistance, Vector3 startingPosition, Vector3 perpendicular, Vector3 direction, float roadSize, GameObject parent)
	{
		float currentIncrement = 0;

		while (currentIncrement < finalDistance)
		{
			int randomIndex = Random.Range(0, city.Buildings.Count);
			Vector3 colliderBounds = city.Buildings[randomIndex].GetComponent<MeshRenderer>().bounds.extents;
			Vector3 positionToCheck = startingPosition + (perpendicular * (city.SideWalkWidth + city.SpaceBetweenSideAndBuilding + colliderBounds.z + roadSize)) + direction * (currentIncrement + colliderBounds.x);
			Quaternion rotation = Quaternion.Euler(0.0f, Vector3.SignedAngle(-Vector3.forward, -perpendicular, Vector3.up), 0.0f);
			currentIncrement += colliderBounds.x;

			Collider[] colliders = Physics.OverlapBox(positionToCheck, colliderBounds, rotation);
			
			// If we don't have any collisions, spawn a building
			if (colliders.Length == 0)
			{
				GameObject spawnedObject = GameObject.Instantiate(city.Buildings[randomIndex]);
				spawnedObject.transform.position = positionToCheck;
				spawnedObject.transform.rotation = rotation;
				spawnedObject.transform.parent = parent.transform;

				currentIncrement += colliderBounds.x + city.SpaceBetweenBuildings;
			}
			else
			{
				currentIncrement += city.SpaceBetweenBuildings;
			}
		}
	}

	/// <summary>
	/// Returns point of intersection. If there is none, return the offset point.
	/// </summary>
	/// <param name="right"></param>
	/// <param name="left"></param>
	/// <param name="intersection"></param>
	/// <returns></returns>
	private void GetRoadPoint(RoadSegment right, RoadSegment left, Intersection intersection, List<Vector3> vertices)
	{
		// Direction from intersections along both roads
		Vector3 rightDirection = right.GetOtherSide(intersection).Position - intersection.Position;
		Vector3 leftDirection = left.GetOtherSide(intersection).Position - intersection.Position;

		float rightOffset = right.GetRoadEdgeOffset(right.GetOtherSide(intersection));
		float leftOffset = left.GetRoadEdgeOffset(intersection);

		// Find offset point that is perpendicular to the direction. Offset is half the road width.
		// This point is on the edge of the road
		Vector3 rightPoint = new Vector3(-rightDirection.z, 0.0f, rightDirection.x);
		rightPoint = rightPoint.normalized * rightOffset;

		Vector3 leftPoint = new Vector3(leftDirection.z, 0.0f, -leftDirection.x);
		leftPoint = leftPoint.normalized * leftOffset;

		Vector3 intersectionPoint;

		if(!Mathf.Approximately(leftOffset, rightOffset) && Vector3.Angle(rightDirection, leftDirection) > 160.0f)
		{
			// The offset of the road edges are different and they are almost in a straight line
			// Create a circle at the center of the intersection with a radius the size of the bigger offset (+ 0.1f to avoid floating point errors causing missed tangents)
			// When the road edge collides with that circle, that is where it will be part of the intersection
			float radius = 0.0f;
			if(rightOffset < leftOffset)
			{
				radius = leftOffset + 0.1f;
			}
			else
			{
				radius = rightOffset + 0.1f;
			}

			intersectionPoint = GetCircleIntersection(radius, leftDirection, leftPoint, Vector3.zero);
			left.SetVertices(intersection, intersectionPoint, true);
			vertices.Add(intersectionPoint);

			intersectionPoint = GetCircleIntersection(radius, rightDirection, rightPoint, Vector3.zero);
			right.SetVertices(intersection, intersectionPoint, false);
			vertices.Add(intersectionPoint);
		}
		else
		{
			// If the offset for both edges is the same, find the intersection
			if (GetLineIntersection(out intersectionPoint, rightPoint, rightDirection, leftPoint, leftDirection))
			{
				right.SetVertices(intersection, intersectionPoint, false);
				left.SetVertices(intersection, intersectionPoint, true);
				vertices.Add(intersectionPoint);
			}
			else
			{
				right.SetVertices(intersection, rightPoint, false);
				left.SetVertices(intersection, rightPoint, true);
				vertices.Add(rightPoint);
			}
		}


	}

	/// <summary>
	/// Returns the point of the road where it reaches the intersetion
	/// </summary>
	/// <param name="segment"></param>
	/// <param name="intersection"></param>
	/// <param name="right"></param>
	private void GetRoadPoint(RoadSegment segment, Intersection intersection, bool right)
	{
		Vector3 direction = segment.GetOtherSide(intersection).Position - intersection.Position;

		Vector3 point;
		if (right)
		{
			point = new Vector3(direction.z, 0.0f, -direction.x);
			point = point.normalized * segment.GetRoadEdgeOffset(intersection);
		}
		else
		{
			point = new Vector3(-direction.z, 0.0f, direction.x);
			point = point.normalized * segment.GetRoadEdgeOffset(segment.GetOtherSide(intersection));
		}

		segment.SetVertices(intersection, point, right);
	}

	/// <summary>
	/// Gets an intersection between two lines
	/// Since all y values are 0, we don't have to do a coplanar check
	/// </summary>
	/// <param name="intersectionPoint"></param>
	/// <param name="linePointA"></param>
	/// <param name="lineDirectionA"></param>
	/// <param name="linePointB"></param>
	/// <param name="lineDirectionB"></param>
	/// <returns></returns>
	private bool GetLineIntersection(out Vector3 intersectionPoint, Vector3 linePointA, Vector3 lineDirectionA, Vector3 linePointB, Vector3 lineDirectionB)
	{
		Vector3 differenceVector = linePointB - linePointA;
		Vector3 crossA_B = Vector3.Cross(lineDirectionA, lineDirectionB);
		Vector3 crossDiffAndB = Vector3.Cross(differenceVector, lineDirectionB);

		float planar = Vector3.Dot(differenceVector, crossA_B);

		// Check for skew lines and parallel lines
		if (Mathf.Abs(planar) < 0.0001f && crossA_B.sqrMagnitude > 0.0001f)
		{
			intersectionPoint = linePointA + (lineDirectionA * Vector3.Dot(crossDiffAndB, crossA_B) / crossA_B.sqrMagnitude);
			return true;
		}
		else
		{
			// No intersection
			intersectionPoint = Vector3.zero;
			return false;
		}
	}

	/// <summary>
	/// Gets the point where a line touches a circle
	/// </summary>
	/// <param name="radius"></param>
	/// <param name="direction"></param>
	/// <param name="point"></param>
	/// <param name="circlePosition"></param>
	/// <returns></returns>
	private Vector3 GetCircleIntersection(float radius, Vector3 direction, Vector3 point, Vector3 circlePosition)
	{
		Vector3 normalizedDirection = direction.normalized;
		float a = normalizedDirection.x * normalizedDirection.x + normalizedDirection.z * normalizedDirection.z;
		float b = 2 * (normalizedDirection.x * (point.x - circlePosition.x) + normalizedDirection.z * (point.z - circlePosition.z));
		float c = (point.x - circlePosition.x) * (point.x - circlePosition.x) +
			(point.z - circlePosition.z) * (point.z - circlePosition.z) - radius * radius;
		float determinant = b * b - 4 * a * c;

		if(a <= 0.00001 || determinant < 0)
		{
			// No intersection
			// Shouldn't happen when creating intersection meshes
			return Vector3.zero;
		}
		else if (Mathf.Approximately(determinant, 0.0f))
		{
			// Tangent line
			float t = -b / (2 * a);
			return new Vector3(point.x + t * direction.x, 0.0f, point.z + t * direction.z);
		}
		else
		{
			float t = (-b + Mathf.Sqrt(determinant)) / (2 * a);
			Vector3 intersection1 = new Vector3(point.x + t * normalizedDirection.x, 0.0f, point.z + t * normalizedDirection.z);
			t = (-b - Mathf.Sqrt(determinant)) / (2 * a);
			Vector3 intersection2 = new Vector3(point.x + t * normalizedDirection.x, 0.0f, point.z + t * normalizedDirection.z);

			if(Vector3.Distance(direction, intersection1) < Vector3.Distance(direction, intersection2))
			{
				return intersection1;
			}
			else
			{
				return intersection2;
			}
		}
	}

	#endregion
}

/// <summary>
/// Helper class for creating sidewalks
/// </summary>
public struct SidewalkCheckHelper
{
	public Intersection Intersection;
	public RoadSegment Road;
}