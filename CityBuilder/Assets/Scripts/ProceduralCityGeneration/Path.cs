using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path
{
	private Vector3 positionStart;
	public Vector3 PositionStart
	{
		get => positionStart;
		set { positionStart = value; UpdateValues(); }
	}

	private Vector3 positionEnd;
	public Vector3 PositionEnd
	{
		get => positionEnd;
		set { positionEnd = value; UpdateValues(); }
	}

	private GameObject lineRenderer;
	public GameObject LineRenderer { get => lineRenderer; set => lineRenderer = value; }

	private Vector3 direction;
	public Vector3 Direction { get => direction; }

	private Vector3 perpendicular;
	public Vector3 Perpendicular { get => perpendicular; }

	private float length;
	public float Length { get => length; }

	public Path(Vector3 start, Vector3 end, GameObject lineObject)
	{
		positionStart = start;
		positionEnd = end;

		UpdateValues();

		lineRenderer = lineObject;
		VisualizeRoad();
	}

	// Code to visualize roads.
	// Right now using line renderers for prototype.
	// Change to mesh generation later.
	private void VisualizeRoad()
	{
		LineRenderer line = lineRenderer.GetComponent<LineRenderer>();
		line.SetPosition(0, positionStart);
		line.SetPosition(1, positionEnd);
	}

	private void UpdateValues()
	{
		direction = positionStart - positionEnd;
		length = direction.magnitude;
		direction = direction.normalized;
		perpendicular = new Vector3(-direction.z, 0.0f, direction.x).normalized;
	}
}
