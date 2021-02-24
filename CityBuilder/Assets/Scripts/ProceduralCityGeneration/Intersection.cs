using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Intersection : ScriptableObject
{
	/// <summary>
	/// Position of this intersection
	/// </summary>
	[SerializeField]
	private Vector3 position;
	public Vector3 Position { get => position; set => position = value; }

	/// <summary>
	/// Intersections that are created from this intersection
	/// </summary>
	[SerializeField]
	private List<RoadSegment> roads = null;
	public List<RoadSegment> Roads { get => roads; set => roads = value; }

	/// <summary>
	/// Creates an intersection and initializes the values
	/// </summary>
	/// <param name="position"></param>
	/// <param name="city"></param>
	/// <returns></returns>
	public static Intersection CreateIntersection(Vector3 position, CityInfo city)
	{
		Intersection init = ScriptableObject.CreateInstance<Intersection>();
		init.position = position;
		init.roads = new List<RoadSegment>();
		AssetDatabase.AddObjectToAsset(init, city);
		return init;
	}

	/// <summary>
	/// Returns the road segment connected to the specified intersection
	/// </summary>
	/// <param name="intersection"></param>
	/// <returns></returns>
	public RoadSegment ConnectedTo(Intersection intersection)
	{
		for(int i = 0; i < roads.Count; i++)
		{
			if(intersection == roads[i].GetOtherSide(this))
			{
				return roads[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Removes connection to the specified intersection on this side of the connection
	/// </summary>
	/// <param name="intersection"></param>
	public void RemoveConnection(RoadSegment road)
	{
		if(road.ContainsIntersection(this))
		{
			road.GetOtherSide(this).roads.Remove(road);
			roads.Remove(road);
			DestroyImmediate(road, true);
		}
		else
		{
			Debug.Log("Attempting to remove road that does not belong to this intersection.");
		}
	}

	/// <summary>
	/// Creates a new intersection connected to this one
	/// </summary>
	/// <param name="position"></param>
	/// <param name="city"></param>
	/// <returns></returns>
	public Intersection CreateNewIntersection(Vector3 position, CityInfo city)
	{
		Intersection intersection = Intersection.CreateIntersection(position, city);
		city.Nodes.Add(intersection);
		ConnectToIntersection(intersection, city);
		AssetDatabase.SaveAssets();
		return intersection;
	}

	/// <summary>
	/// Creates the road segment that merges the intersections
	/// </summary>
	/// <param name="first"></param>
	/// <param name="second"></param>
	public void ConnectToIntersection(Intersection toConnect, CityInfo city)
	{
		RoadSegment road = CreateInstance<RoadSegment>();
		AssetDatabase.AddObjectToAsset(road, city);
		road.ConnectionPointA = this;
		road.ConnectionPointB = toConnect;

		Roads.Add(road);
		toConnect.Roads.Add(road);
	}

	/// <summary>
	/// Merges two intersections, making sure connections from the second all exist in the first before deleting it.
	/// </summary>
	/// <param name="toMerge"></param>
	public void MergeIntersection(Intersection toMerge, CityInfo city)
	{
		HashSet<Intersection> alreadyConnected = new HashSet<Intersection>();

		// Add all intersections that the one we are merging is already connected to
		// so that we don't create duplicate road segments
		alreadyConnected.Add(this);

		for (int i = 0; i < Roads.Count; i++)
		{
			if (Roads[i].GetOtherSide(this) == toMerge)
			{
				DestroyImmediate(Roads[i], true);
				Roads.RemoveAt(i);
				i--;
			}
			else
			{
				alreadyConnected.Add(Roads[i].GetOtherSide(this));
			}
		}

		// Create the roads where the first intersection isn't connected.
		// Delete the existing roads
		while (toMerge.Roads.Count > 0)
		{
			if (!alreadyConnected.Contains(toMerge.Roads[0].GetOtherSide(toMerge)))
			{
				ConnectToIntersection(toMerge.Roads[0].GetOtherSide(toMerge), city);
			}

			toMerge.RemoveConnection(toMerge.Roads[0]);
		}
		city.Nodes.Remove(toMerge);
		DestroyImmediate(toMerge, true);
	}

	/// <summary>
	/// Removes all roads that are connected to this intersection
	/// </summary>
	public void DeleteAllConnections()
	{
		for (int i = 0; i < Roads.Count; i++)
		{
			Roads[i].GetOtherSide(this).Roads.Remove(Roads[i]);
			DestroyImmediate(Roads[i], true);
		}
	}

	/// <summary>
	/// Splits a road and creates an intersection in the middle connecting to both the intersections of the original road
	/// </summary>
	/// <param name="toSplit"></param>
	/// <param name="city"></param>
	public void SplitRoad(RoadSegment toSplit, CityInfo city)
	{
		if(toSplit.ContainsIntersection(this))
		{
			Intersection otherSide = toSplit.GetOtherSide(this);
			RemoveConnection(toSplit);

			Intersection between = CreateIntersection((position + otherSide.position) / 2, city);
			city.Nodes.Add(between);

			ConnectToIntersection(between, city);
			otherSide.ConnectToIntersection(between, city);

		}
		else
		{
			Debug.Log("Attempting to split road that does not belong to this intersection.");
		}
	}
}