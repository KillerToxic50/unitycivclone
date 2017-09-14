using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hex {

	public int Q;
	public int R;
	public int S;
	public GameObject gameObject;
	public HexComponent hexComponent;
	public HexMap hexMap;
	public float elevation;
	public float temperature;
	public int elevationLevel;
	public int temperatureLevel;
	public Unit occupyingUnit;
	public City belongsTo;

	static float radius = 1;
	static float height = radius * 2;
	static float width = (Mathf.Sqrt(3)/2) * 2;
	static float vertical = height * 3/4;
	static float horizontal = width;

	/// <summary>
	/// Initializes a new instance of the <see cref="Hex"/> class.
	/// </summary>
	/// <param name="q">Q.</param>
	/// <param name="r">The red component.</param>
	public Hex (int q, int r) {
		Q = q;
		R = r;
		S = -q + -r;
	}

	public override bool Equals (System.Object obj) {
		if (obj == null)
			return false;
		Hex h = obj as Hex;
		if ((System.Object)h == null)
			return false;
		return (h.Q == Q) && (h.R == R) && (h.S == S);
	}

	public bool Equals (Hex h) {
		if ((System.Object)h == null)
			return false;
		return (h.Q == Q) && (h.R == R) && (h.S == S);
	}

	/// <summary>
	/// Generates world position from map coordinates
	/// </summary>
	/// <returns>Position in world space.</returns>
	public Vector3 GetWorldPosition () {
		float X = Q;
		float Z = R;

		X += Z/2;

		X *= horizontal;
		Z *= vertical;

		return new Vector3 (X, 0, Z);
	}

	/// <summary>
	/// Sets the hex type and updates visuals.
	/// </summary>
	public void SetType () {
		if (elevation < 0.4f) {
			// Ocean
			elevationLevel = 0;
		} else if (elevation > 0.8f) {
			// Mountain
			elevationLevel = 3;
		} else if (elevation > 0.6f) {
			// Hill
			elevationLevel = 2;
		} else {
			// Flat
			elevationLevel = 1;
		}

		if (temperature < 0.2f) {
			// Tundra
			temperatureLevel = 0;
		} else if (temperature > 0.7f) {
			// Desert
			temperatureLevel = 2;
		} else {
			// Plains
			temperatureLevel = 1;
		}

		hexComponent.SetVisuals(elevationLevel, temperatureLevel);
	}

	/// <summary>
	/// Distance between self and a specified hex in tiles.
	/// </summary>
	/// <param name="otherHex">Hex to find distance between.</param>
	public int Distance (Hex otherHex) {
		return Mathf.Max (
			Mathf.Abs(Q - otherHex.Q),
			Mathf.Abs(R - otherHex.R),
			Mathf.Abs(S - otherHex.S)
			);
	}

	public void DoWrapping(Vector3 cameraPos, int width) {
		float mapWidth = (width * horizontal);
		Vector3 p = GetWorldPosition();
		int mapWidthsFromCamera = Mathf.RoundToInt((p.x - cameraPos.x) / mapWidth);
		p.x -= mapWidthsFromCamera*mapWidth;
		gameObject.transform.position = p;
	}

	/// <summary>
	/// Returns hexes within a given radius.
	/// </summary>
	/// <returns>A list of hexes.</returns>
	/// <param name="radius">Radius.</param>
	public List<Hex> GetHexesWithinRadius(int radius)
	{
		radius++;
		List<Hex> results = new List<Hex> ();
		for (int dRow = -radius; dRow < radius-1; dRow++) {
			for (int dCol = Mathf.Max (-radius+1, -dRow - radius); dCol < Mathf.Min (radius, -dRow + radius-1); dCol++) {
				Hex h = hexMap.GetHexAt(Q + dRow + 1, R + dCol);
				if ( h != null)
					results.Add(h);
			}
		}
		return results;
	}

	/// <summary>
	/// Returns the adjacent hexes.
	/// </summary>
	/// <returns>A list of hexes.</returns>
	/// <param name="includeSelf">If set to <c>true</c>, also returns self.</param>
	public List<Hex> GetAdjacentHexes(bool includeSelf = false) {
		List<Hex> result = GetHexesWithinRadius (1);
		if (!includeSelf)
			result.Remove (this);
		return result;
	}
}