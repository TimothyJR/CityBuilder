using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class Sidewalk : MonoBehaviour
{
	/// <summary>
	/// The original vertices of the sidewalk
	/// </summary>
	[SerializeField]
	private List<Vector3> originalVertices = new List<Vector3>();
	public List<Vector3> OriginalVertices { get => originalVertices; set => originalVertices = value; }

	/// <summary>
	/// The material to use for the sidewalk
	/// </summary>
	[SerializeField]
	private Material sidewalkMaterial = null;
	public Material SidewalkMaterial { get => sidewalkMaterial; set => sidewalkMaterial = value; }

	/// <summary>
	/// The material to use for the mesh enclosed by the sidewalk
	/// </summary>
	[SerializeField]
	private Material innerMaterial = null;
	public Material InnerMaterial { get => innerMaterial; set => innerMaterial = value; }

	/// <summary>
	/// List of all vertices in the mesh
	/// First list is the vertices along the road
	/// Second list is the vertices for the inner part of the trim
	/// Third list is the vertices going up from the trim
	/// Fourth list is the vertices inner edge of the sidewalk
	/// </summary>
	[SerializeField]
	private List<Vector3>[] vertices = new List<Vector3>[4];

	/// <summary>
	/// The direction to offset each level of the sidewalk going away from the roads
	/// </summary>
	[SerializeField]
	private List<Vector3> sidewalkDirections = new List<Vector3>();

	/// <summary>
	/// The mesh that is enclosed by the sidewalk
	/// </summary>
	[SerializeField]
	private GameObject innerBlock = null;

	/// <summary>
	/// The width of the road trim
	/// </summary>
	[SerializeField] private float sideWalkTrimWidth = 0.3f;

	/// <summary>
	/// How high the sidewalk is
	/// </summary>
	[SerializeField] private float sideWalkHeight = 0.12f;

	/// <summary>
	/// The width of the sidewalk
	/// </summary>
	[SerializeField] private float sideWalkWidth = 1.2f;

	/// <summary>
	/// Draw debug lines
	/// </summary>
	//private void Update()
	//{
	//	if (vertices[0] != null)
	//	{
	//		for (int i = 0; i < vertices[0].Count; i++)
	//		{
	//			Debug.DrawLine(vertices[0][i] + transform.position, vertices[1][i] + transform.position, Color.blue);
	//			Debug.DrawLine(vertices[1][i] + transform.position, vertices[2][i] + transform.position, Color.blue);
	//			Debug.DrawLine(vertices[2][i] + transform.position, vertices[3][i] + transform.position, Color.blue);
	//	
	//			Debug.DrawLine(vertices[0][i] + transform.position, vertices[0][(i + 1) % vertices[0].Count] + transform.position, Color.red);
	//			Debug.DrawLine(vertices[1][i] + transform.position, vertices[1][(i + 1) % vertices[1].Count] + transform.position, Color.red);
	//			Debug.DrawLine(vertices[2][i] + transform.position, vertices[2][(i + 1) % vertices[2].Count] + transform.position, Color.red);
	//			Debug.DrawLine(vertices[3][i] + transform.position, vertices[3][(i + 1) % vertices[3].Count] + transform.position, Color.red);
	//		}
	//	}
	//}

	/// <summary>
	/// Adjusts the position of the vertices for the mesh position
	/// </summary>
	public void FixMeshOffset()
	{
		for(int i = 0; i < originalVertices.Count; i++)
		{
			originalVertices[i] -= transform.position;
		}
	}

	/// <summary>
	/// Finds the direction between both edges of the sidewalk point towards the inside of the mesh
	/// </summary>
	public void FindDirection()
	{
		for(int i = 0; i < originalVertices.Count; i++)
		{
			Vector3 direction1 = originalVertices[i] - originalVertices[Modulo(i - 1, originalVertices.Count)];
			Vector3 direction2 = originalVertices[i] - originalVertices[Modulo(i + 1, originalVertices.Count)];

			float angle = Vector3.SignedAngle(direction1, direction2, Vector3.up) / 2;
			if(angle < 0)
			{
				angle += 360;
			}

			direction2 = Quaternion.AngleAxis(angle, Vector3.up) * direction1;

			direction1 = new Vector3(-direction1.z, 0.0f, direction1.x);
			if(Vector3.Dot(direction1, direction2) > 0)
			{
				sidewalkDirections.Add(direction2.normalized);
			}
			else
			{
				sidewalkDirections.Add(-direction2.normalized);
			}
		}
	}

	/// <summary>
	/// Creates the vertices of the sidewalk
	/// Creates points for the trim, going up the height of the sidewalk and then the width of the sidewalk
	/// </summary>
	public void CalculateVertices()
	{
		for(int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = new List<Vector3>();
		}

		vertices[0] = originalVertices;

		for(int i = 0; i < vertices[0].Count; i++)
		{
			vertices[1].Add(vertices[0][i] + sidewalkDirections[i] * sideWalkTrimWidth);
			vertices[2].Add(vertices[1][i] + Vector3.up * sideWalkHeight);
			vertices[3].Add(vertices[2][i] + sidewalkDirections[i] * sideWalkWidth);
		}
	}

	/// <summary>
	/// Creates the sidewalk mesh
	/// </summary>
	public void CreateMesh()
	{
		Mesh mesh = new Mesh();
		Vector3[] finalVertices;
		int[] triangles;
		if (sideWalkTrimWidth > 0)
		{
			finalVertices = new Vector3[vertices[0].Count * 4];
			finalVertices = vertices[0].Concat(vertices[1]).Concat(vertices[2]).Concat(vertices[3]).ToArray();
			triangles = new int[(int)(finalVertices.Length * 4.5f)];

			// Create squares going around the perimeter
			for(int i = 0; i < vertices[0].Count; i++)
			{
				// For each piece of the perimeter, create three quads for the width of the sidewalk
				int index = i * 18;
				for(int j = 0; j < 3; j++)
				{
					triangles[index + (6 * j)] = i + (vertices[0].Count * j);
					triangles[index + 1 + (6 * j)] = i + vertices[0].Count * (j + 1);
					triangles[index + 2 + (6 * j)] = (i + 1) % vertices[0].Count + (vertices[0].Count * j);
					triangles[index + 3 + (6 * j)] = triangles[index + 2 + (6 * j)];
					triangles[index + 4 + (6 * j)] = triangles[index + 1 + (6 * j)];
					triangles[index + 5 + (6 * j)] = (i + 1) % vertices[0].Count + (vertices[0].Count * (j + 1));
				}
			}
		}
		else
		{
			// If we don't have a trim width, we skip using the vertices for it
			finalVertices = new Vector3[vertices[0].Count * 3];
			finalVertices = vertices[1].Concat(vertices[2]).Concat(vertices[3]).ToArray();
			triangles = new int[finalVertices.Length * 4];

			for(int i = 0; i < vertices[0].Count; i++)
			{
				int index = i * 12;
				for(int j = 0; j < 2; j++)
				{
					triangles[index + (6 * j)] = i + (vertices[0].Count * j);
					triangles[index + 1 + (6 * j)] = i + vertices[0].Count * (j + 1);
					triangles[index + 2 + (6 * j)] = (i + 1) % vertices[0].Count + (vertices[0].Count * j);
					triangles[index + 3 + (6 * j)] = triangles[index + 2 + (6 * j)];
					triangles[index + 4 + (6 * j)] = triangles[index + 1 + (6 * j)];
					triangles[index + 5 + (6 * j)] = (i + 1) % vertices[0].Count + (vertices[0].Count * (j + 1));
				}
			}
		}

		mesh.vertices = finalVertices;
		mesh.triangles = triangles;

		MeshRenderer renderer = GetComponent<MeshRenderer>();

		if (renderer == null)
		{
			renderer = gameObject.AddComponent<MeshRenderer>();
		}

		renderer.material = sidewalkMaterial;

		MeshFilter filter = GetComponent<MeshFilter>();

		if(filter == null)
		{
			filter = gameObject.AddComponent<MeshFilter>();
		}

		filter.mesh = mesh;

		MeshCollider collider = GetComponent<MeshCollider>();

		if(collider == null)
		{
			collider = gameObject.AddComponent<MeshCollider>();
		}

		collider.sharedMesh = mesh;
	}

	/// <summary>
	/// Creates the fill mesh for the sidewalks
	/// </summary>
	public void CreateFillMesh()
	{
		if(innerBlock == null)
		{
			innerBlock = new GameObject();
			innerBlock.transform.parent = transform;
			innerBlock.transform.localPosition = Vector3.zero;
			innerBlock.AddComponent<MeshRenderer>().material = innerMaterial;
			innerBlock.AddComponent<MeshFilter>();
			innerBlock.AddComponent<MeshCollider>();
		}

		Vector3[] innerVertices = vertices[0].ToArray();

		for(int i = 0; i < innerVertices.Length; i++)
		{
			innerVertices[i] += sidewalkDirections[i] * (sideWalkTrimWidth + 0.1f);
			innerVertices[i] = new Vector3(innerVertices[i].x, sideWalkHeight - 0.01f, innerVertices[i].z);
		}

		Stack<int> pointToCheck = new Stack<int>();
		List<int> triangles = new List<int>();

		pointToCheck.Push(0);
		for(int i = 2; i < innerVertices.Length; i++)
		{
			if(!FindInnerTriangles(pointToCheck, innerVertices, triangles, i, i - 1))
			{
				pointToCheck.Push(i - 1);
			}
		}

		// Close the mesh
		if(triangles.Count > 2)
		{
			triangles.Add(innerVertices.Length - 1);
			if (triangles[triangles.Count - 2] == 0)
			{
				triangles.Add(triangles[triangles.Count - 4]);
			}
			else
			{
				triangles.Add(triangles[triangles.Count - 2]);
			}
			triangles.Add(0);
		}


		Mesh mesh = new Mesh();
		mesh.vertices = innerVertices;
		mesh.triangles = triangles.ToArray();

		innerBlock.GetComponent<MeshFilter>().mesh = mesh;
		innerBlock.GetComponent<MeshCollider>().sharedMesh = mesh;
	}

	/// <summary>
	/// Finds the triangles to create a mesh in an enclosed set of line segments
	/// </summary>
	/// <param name="pointToCheck"></param>
	/// <param name="innerVertices"></param>
	/// <param name="triangles"></param>
	/// <param name="vertexIndex"></param>
	/// <param name="connectToIndex"></param>
	/// <returns></returns>
	public bool FindInnerTriangles(Stack<int> pointToCheck, Vector3[] innerVertices, List<int> triangles, int vertexIndex, int connectToIndex)
	{
		int index = pointToCheck.Peek();

		// Check if the vector pointing towards the check point is towards the inside of the mesh
		if (!IsVectorBetween(innerVertices[vertexIndex - 1] - innerVertices[vertexIndex], innerVertices[(vertexIndex + 1) % innerVertices.Length] - innerVertices[vertexIndex], innerVertices[index] - innerVertices[vertexIndex]))
		{
			return false;
		}

		for (int j = 0; j < innerVertices.Length; j++)
		{
			// Check to see if the vector poitning towards the check point intersects with any of the walls
			if (vertexIndex != j && vertexIndex != j + 1 && j != index && (j + 1) % innerVertices.Length != index)
			{
				if(!ClearLineToPoint(innerVertices[index], innerVertices[vertexIndex], innerVertices[j], innerVertices[(j + 1) % innerVertices.Length]))
				{
					return false;
				}
			}
		}

		triangles.Add(vertexIndex);
		triangles.Add(connectToIndex);
		triangles.Add(index);

		if (index != 0)
		{
			pointToCheck.Pop();
			if (!FindInnerTriangles(pointToCheck, innerVertices, triangles, vertexIndex, index))
			{
				pointToCheck.Push(index);
			}
		}

		return true;
	}

	/// <summary>
	/// Return true if vector c is between a and b (Only uses the x and z axis)
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="c"></param>
	/// <returns></returns>
	private bool IsVectorBetween(Vector3 a, Vector3 b, Vector3 c)
	{
		if(GetClockwiseAngle(a, b) > GetClockwiseAngle(a, c))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Returns the angle between two vectors going clockwise around the up vector
	/// The vectors need to be on the xz plane
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	private float GetClockwiseAngle(Vector3 a, Vector3 b)
	{
		float angle = Vector3.Angle(a, b);
		float sign = Mathf.Sign(Vector3.Dot(Vector3.up, Vector3.Cross(a, b)));

		if(sign < 0)
		{
			angle = 360 - angle;
		}

		return angle;
	}

	/// <summary>
	/// Checks if a line segment from point a to b does not intersect a line segment from point c to d in the x and z axis
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="c"></param>
	/// <param name="d"></param>
	/// <returns></returns>
	private bool ClearLineToPoint(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		float denom = (d.z - c.z) * (b.x - a.x) - (d.x - c.x) * (b.z - a.z);
		if(Mathf.Approximately(0.0f, denom))
		{
			return true;
		}
		else
		{
			float timeA = ((d.x - c.x) * (a.z - c.z) - (d.z - c.z) * (a.x - c.x)) / denom;
			float timeB = ((b.x - a.x) * (a.z - c.z) - (b.z - a.z) * (a.x - c.x)) / denom;

			if(timeA >= 0.0f && timeA <= 1.0f && timeB >= 0.0f && timeB <= 1.0f)
			{
				return false;
			}

			return true;
		}
	}

	/// <summary>
	/// Custom modulo to handle negatives since the % operator does not
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	private int Modulo(int a, int b)
	{
		int value = (a %= b) < 0 ? a + b : a;
		return value;
	}
}