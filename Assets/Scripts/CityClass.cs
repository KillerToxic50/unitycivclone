using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City {

	public HexMap hexMap;
	public GameObject gameObject;
	public CityComponent cityComponent;

	public List<Hex> hexes = new List<Hex>();

	public int productionPerTurn = 1;
	public int currentProductionCost = 10;
	public int production = 0;

	public City (Hex spawnHex) {
		hexes.Add (spawnHex);
	}

	public void DoTurn () {
		if (currentProductionCost == 0) {
			// Need to assign production
		}
		else if (production == currentProductionCost) {
			hexMap.SpawnUnit(this, 0);
			production = 0;
		}
		else {
			production += productionPerTurn;
		}
	}
}
