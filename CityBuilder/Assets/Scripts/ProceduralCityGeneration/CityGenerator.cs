using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CityGenerator : MonoBehaviour
{
	/// <summary>
	/// City that is being edited
	/// </summary>
	[SerializeField]
	private CityInfo city = null;
	public CityInfo City { get => city; set => city = value; }
}