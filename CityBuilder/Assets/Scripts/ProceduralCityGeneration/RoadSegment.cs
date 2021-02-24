using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoadSegment : ScriptableObject
{
	/// <summary>
	/// The first intersection of this road
	/// </summary>
	[SerializeField]
	private Intersection connectionPointA;
	public Intersection ConnectionPointA { get => connectionPointA; set => connectionPointA = value; }

	/// <summary>
	/// The second intersection of this road
	/// </summary>
	[SerializeField]
	private Intersection connectionPointB;
	public Intersection ConnectionPointB { get => connectionPointB; set => connectionPointB = value; }

	/// <summary>
	/// Holds vertices for creating the mesh.
	/// Index 0 and 1 are for intersection A
	/// Index 2 and 3 are for intersection B
	/// First index is the right side from the intersection. Second is the left.
	/// Triangle order will be 0, 1, 2 and 0, 2, 3
	/// </summary>
	private Vector3[] vertices = new Vector3[4];
	public Vector3[] Vertices { get => vertices; }

	/// <summary>
	/// The width of the road
	/// </summary>
	[SerializeField]
	private float roadWidth = 8.0f;
	public float RoadWidth { get => roadWidth; set => roadWidth = Mathf.Max(value, 0.1f); }

	/// <summary>
	/// Offset of the road to the right of intersection A
	/// </summary>
	[SerializeField]
	private float roadOffset = 0.0f;
	public float RoadOffset{ get => roadOffset; }

	/// <summary>
	/// Returns the other intersection of this road
	/// </summary>
	/// <param name="intersection"></param>
	/// <returns></returns>
	public Intersection GetOtherSide(Intersection intersection)
	{
		if (connectionPointA == intersection)
		{
			return connectionPointB;
		}
		else
		{
			return connectionPointA;
		}
	}

	/// <summary>
	/// Sets the vertex for the side of the road of the intersection.
	/// </summary>
	/// <param name="intersection"></param>
	/// <param name="vertex"></param>
	/// <param name="right"></param>
	public void SetVertices(Intersection intersection, Vector3 vertex, bool right)
	{
		if (connectionPointA == intersection)
		{
			if (right)
			{
				vertices[0] = vertex;
			}
			else
			{
				vertices[1] = vertex;
			}
		}
		else
		{
			if (right)
			{
				vertices[2] = vertex;
			}
			else
			{
				vertices[3] = vertex;
			}
		}
	}

	/// <summary>
	/// Returns the vertex for the side of the road of the intersection
	/// </summary>
	/// <param name="intersection"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public Vector3 GetVertex(Intersection intersection, bool right)
	{
		if (connectionPointA == intersection)
		{
			if (right)
			{
				return vertices[0];
			}
			else
			{
				return vertices[1];
			}
		}
		else
		{
			if (right)
			{
				return vertices[2];
			}
			else
			{
				return vertices[3];
			}
		}
	}

	/// <summary>
	/// Returns a vertex in its world position
	/// </summary>
	/// <param name="intersection"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public Vector3 GetVertexWorldPosition(Intersection intersection, bool right)
	{
		Vector3 offset = (connectionPointA.Position + connectionPointB.Position) / 2;

		if (connectionPointA == intersection)
		{
			if (right)
			{
				return vertices[0] + offset;
			}
			else
			{
				return vertices[1] + offset;
			}
		}
		else
		{
			if (right)
			{
				return vertices[2] + offset;
			}
			else
			{
				return vertices[3] + offset;
			}
		}
	}

	/// <summary>
	/// Vertices are originally set based on their position from the intersection point.
	/// The position of the road will be in the middle of the intersections.
	/// The offset added with this function will correct the vertex positions
	/// </summary>
	public void OffsetVertices()
	{
		// Get direction B to A
		Vector3 direction = connectionPointA.Position - connectionPointB.Position;

		// Half the vector since we will offset both pairs of verts by half the distance
		direction /= 2;

		vertices[0] += direction;
		vertices[1] += direction;
		vertices[2] -= direction;
		vertices[3] -= direction;
	}

	/// <summary>
	/// Returns true if the intersection is part of this road
	/// </summary>
	/// <param name="intersection"></param>
	/// <returns></returns>
	public bool ContainsIntersection(Intersection intersection)
	{
		return (connectionPointA == intersection || connectionPointB == intersection);
	}

	/// <summary>
	/// Keeps the offset between the values of -width and width
	/// Used by editor scripts so that it will always set the proper value since the editor will display different values depending on the intersection selected
	/// </summary>
	/// <param name="value"></param>
	/// <param name="intersection"></param>
	public void SetRoadOffset(float value, Intersection intersection)
	{
		if(intersection == connectionPointA)
		{
			roadOffset = Mathf.Min(-value, roadWidth / 2);
			roadOffset = Mathf.Max(roadOffset, -roadWidth / 2);
		}
		else
		{
			roadOffset = Mathf.Min(value, roadWidth / 2);
			roadOffset = Mathf.Max(roadOffset, -roadWidth / 2);
		}
	}

	/// <summary>
	/// Gets the offset of the road
	/// Used by editor script so that the selected intersection will always display positive for the direction to the right when looking down the road from the interseciton
	/// </summary>
	/// <param name="intersection"></param>
	/// <returns></returns>
	public float GetRoadOffset(Intersection intersection)
	{
		if(intersection == connectionPointA)
		{
			return -roadOffset;
		}
		else
		{
			return roadOffset;
		}
	}

	/// <summary>
	/// Returns the combined offset of the right side of the road from the intersection
	/// </summary>
	/// <param name="intersection"></param>
	/// <returns></returns>
	public float GetRoadEdgeOffset(Intersection intersection)
	{
		if (intersection == connectionPointA)
		{
			return roadWidth / 2 - roadOffset;
		}
		else
		{
			return roadWidth / 2 + roadOffset;
		}
	}
}