using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{

	private List<Path> paths = new List<Path>();

	[SerializeField]
	private int subdivisions = 1;

	[SerializeField]
	private int subdivisionsPerLevel = 1;

	[SerializeField]
	private int numberOfRoads = 5;

	[SerializeField]
	private float minRoadLength = 30.0f;

	[SerializeField]
	private float minSubDivLength = 5.0f;

	[SerializeField]
	private float citySize = 100.0f;

	[SerializeField]
	private float streetSize = 0.6f;

	[SerializeField]
	private float distanceBetweenBuildings = 0.5f;

	[SerializeField]
	private GameObject roadPrefab = null;

	[SerializeField]
	List<GameObject> buildingPrefabs = new List<GameObject>();

	// Start is called before the first frame update
	void Start()
	{
		// Generate initial roads
		for(int i = 0; i < numberOfRoads; i++)
		{
			paths.Add(GenerateRandomPath());
		}

		// Generate subdivisions
		for(int i = 0; i < numberOfRoads; i++)
		{
			PathSubdivide(paths[i], subdivisions, minSubDivLength);
		}

		// Generate Buildings
		for (int i = 0; i < paths.Count; i++)
		{
			GenerateBuildingsOnPath(paths[i], false);
			GenerateBuildingsOnPath(paths[i], true);
		}
	}

	private void GenerateBuildingsOnPath(Path path, bool negative)
	{
		GameObject buildingToSpawn;
		float offset = 0.0f;
		bool atPathEnd = false;
		float side = (negative ? -1 : 1);
		while (!atPathEnd)
		{
			int buildingNumber = Random.Range(0, buildingPrefabs.Count);
			// Create our buildings
			buildingToSpawn = GameObject.Instantiate(buildingPrefabs[buildingNumber]);

			// Get our position
			Collider col = buildingToSpawn.GetComponent<Collider>();
			offset += col.bounds.extents.z;

			if (offset + col.bounds.extents.z * 2 > path.Length)
			{
				atPathEnd = true;
			}
			Vector3 position = path.PositionStart + (side * path.Perpendicular * (col.bounds.extents.x + streetSize)) - (path.Direction * offset);

			// Rotate building to path
			buildingToSpawn.transform.rotation = Quaternion.LookRotation(path.Direction, Vector3.up);

			// Move building onto path
			buildingToSpawn.transform.position = position;
			buildingToSpawn.transform.parent = path.LineRenderer.transform;
			offset += col.bounds.extents.z + distanceBetweenBuildings;
		}
	}

	private void PathSubdivide(Path path, int subdivisionLevels, float minPathLength)
	{
		for(int i = 0; i < subdivisionsPerLevel; i++)
		{
			float position = Random.Range(0.0f, path.Length);
			float length = Random.Range(minSubDivLength, citySize);
			float intersectPoint = Random.Range(0.0f, length);

			Vector3 intersectionPosition = path.PositionEnd + path.Direction * position;
			float angleOfRotation = Random.Range(80.0f, 100.0f);
			Vector3 direction = GetRotated(path.Direction, angleOfRotation);

			Path newPath = new Path(intersectionPosition + direction * (length - intersectPoint), intersectionPosition - direction * intersectPoint, GameObject.Instantiate(roadPrefab));
			paths.Add(newPath);

			if (subdivisionLevels > 0)
			{
				PathSubdivide(newPath, subdivisionLevels - 1, Mathf.Min(minSubDivLength - 10, 10));
			}
		}
	}

	private Vector3 GetRotated(Vector3 vec, float angle)
	{
		float rotate = Mathf.Deg2Rad * angle;
		return new Vector3(vec.x * Mathf.Cos(rotate) - vec.z * Mathf.Sin(rotate), 0.0f, vec.x * Mathf.Sin(rotate) + vec.z * Mathf.Cos(rotate)).normalized;
	}

	private Path GenerateRandomPath()
	{
		Vector3 start = new Vector3(Random.Range(-citySize, citySize), 0.0f, Random.Range(-citySize, citySize));
		Vector3 end = new Vector3(Random.Range(-citySize, citySize), 0.0f, Random.Range(-citySize, citySize));
		Vector3 diff = start - end;

		// At the moment this allows for the city to go outside of the range of city size
		// Has to be fixed
		if (diff.magnitude < minRoadLength)
		{
			float remainderOfDistance = minRoadLength - diff.magnitude;
			start += diff.normalized * (remainderOfDistance / 2);
			end -= diff.normalized * (remainderOfDistance / 2);
		}

		return new Path(start, end, GameObject.Instantiate(roadPrefab));
	}
}
