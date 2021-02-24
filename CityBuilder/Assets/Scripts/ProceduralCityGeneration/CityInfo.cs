using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "City")]
public class CityInfo : ScriptableObject
{
	/// <summary>
	/// All of the intersections of the city
	/// </summary>
	[SerializeField]
	private List<Intersection> nodes = new List<Intersection>();
	public List<Intersection> Nodes { get => nodes; set => nodes = value; }

	/// <summary>
	/// The material to use for the roads
	/// </summary>
	[SerializeField]
	private Material roadMaterial = null;
	public Material RoadMaterial { get => roadMaterial; set => roadMaterial = value; }

	/// <summary>
	/// The material to be used for the sidewalk
	/// </summary>
	[SerializeField]
	private Material sidewalkMaterial = null;
	public Material SidewalkMaterial { get => sidewalkMaterial; set => sidewalkMaterial = value; }

	/// <summary>
	/// The material to be used by the areas enclosed by the sidewalk
	/// </summary>
	[SerializeField]
	private Material innerBlockMaterial = null;
	public Material InnerBlockMaterial { get => innerBlockMaterial; set => innerBlockMaterial = value; }

	/// <summary>
	/// The space to put between each building
	/// </summary>
	[SerializeField]
	private float spaceBetweenBuildings = 1.0f;
	public float SpaceBetweenBuildings { get => spaceBetweenBuildings; set => spaceBetweenBuildings = value; }

	/// <summary>
	/// The space between the sidewalk and the buildings
	/// </summary>
	[SerializeField]
	private float spaceBetweenSideAndBuilding = 1.0f;
	public float SpaceBetweenSideAndBuilding { get => spaceBetweenSideAndBuilding; set => spaceBetweenSideAndBuilding = value; }

	/// <summary>
	/// The width to use for the sidewalks
	/// </summary>
	[SerializeField]
	private float sideWalkWidth = 1.2f;
	public float SideWalkWidth { get => sideWalkWidth; set => sideWalkWidth = value; }

	/// <summary>
	/// A list of the buildings to spawn
	/// </summary>
	[SerializeField]
	private List<GameObject> buildings = new List<GameObject>();
	public List<GameObject> Buildings { get => buildings; set => buildings = value; }

}