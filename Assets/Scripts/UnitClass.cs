using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Unit {
	public UnitComponent unitComponent;
	public GameObject gameObject;
	public Hex currentHex;
	public City belongsTo;

	public Queue<Hex> moveQueue;
	public int moveRange = 2;
	public int movesRemaining;

	private int[] movementCost = new int[] {0, 1, 2, 0};
	private const bool CIV_6_MOVE_RULES = false;

	public Unit () {
		movesRemaining = moveRange;
	}

	/// <summary>
	/// Finds and a path to the specified tile.
	/// </summary>
	/// <param name="targetHex">Hex to find path to.</param>
	public Queue<Hex> FindPathToHex (Hex targetHex, out int turnCost) {
		List<Hex> openList = new List<Hex>();
		List<Hex> closedList = new List<Hex>();
		Dictionary<Hex, Hex> hexMap = new Dictionary<Hex, Hex>();
		Dictionary<Hex, int> hexF = new Dictionary<Hex, int>();	// Total score
		Dictionary<Hex, int> hexG = new Dictionary<Hex, int>();	// Cost from start to hex
		Dictionary<Hex, int> hexH = new Dictionary<Hex, int>(); // Cost from hex to target
		Hex startHex = currentHex;

		Debug.Log ("Pathfinding: Finding path!");

		openList.Add (startHex);
		hexG[startHex] = 0;
		hexH[startHex] = startHex.Distance(targetHex);
		hexF[startHex] = hexG[startHex] + hexH[startHex];

		while (!closedList.Contains (targetHex)) {
			if (openList.Count < 1) {
				Debug.Log ("Pathfinding: No path found!");
				turnCost = 0;
				return null;
			}

			Hex lowestScoreHex = null;
			int lowestScore = 99999999;

			// Get lowest score hex on list
			foreach (Hex h in openList) {
				if (hexF[h] < lowestScore) {
					lowestScore = hexF[h];
					lowestScoreHex = h;
				}
			}

			if (lowestScoreHex == null)
				continue;

			// Remove lowestScoreHex from open list
			openList.Remove (lowestScoreHex);
			closedList.Add (lowestScoreHex);

			// Add adjacent hexes
			foreach (Hex h in lowestScoreHex.GetAdjacentHexes()) {
				// If impassable then add to closed list
				if (movementCost [h.elevationLevel] == 0 || h.occupyingUnit != null) {
					closedList.Add (h);
					openList.Remove (h);
				}

				// Check if not already on closed list
				if (!closedList.Contains (h)) {
					if (openList.Contains (h)) {
						// If already has entries then compare scores
						if (hexG [lowestScoreHex] < hexG [hexMap [lowestScoreHex]]) {
							// If G score lower then update dictionary entries
							hexMap [h] = lowestScoreHex;
							hexG [h] = hexG [hexMap [h]] + movementCost [h.elevationLevel];
							hexH [h] = h.Distance (targetHex);
							hexF [h] = hexG [h] + hexH [h];
						}
					} else {
						// If passable and not on closed list then add to list and set dictionary entries
						openList.Add (h);
						hexMap [h] = lowestScoreHex;
						hexG [h] = hexG [hexMap [h]] + movementCost [h.elevationLevel];
						hexH [h] = h.Distance (targetHex);
						hexF [h] = hexG [h] + hexH [h];
					}
				}
			}
		}

		Debug.Log ("Pathfinding: Path found!");

		Hex pathHex = targetHex;
		Queue<Hex> path = new Queue<Hex>();

		while (!pathHex.Equals(startHex)) {
			//Debug.Log(string.Format("Hex: {0}, {1} -> {2}, {3}", hexMap[pathHex].Q, hexMap[pathHex].R, pathHex.Q, pathHex.R));
			//pathHex.gameObject.GetComponentInChildren<MeshRenderer>().material = null;
			path.Enqueue(pathHex);

			if (!hexMap.ContainsKey (pathHex)) {
				//Debug.LogError (string.Format("Pathfinding: Tile ({0}, {1}) has no parent!", pathHex.Q, pathHex.R));
				turnCost = 0;
				return null;
			}
			if (pathHex.Equals(hexMap [pathHex])) {
				Debug.LogError (string.Format("Pathfinding: Tile ({0}, {1}) references itself!", pathHex.Q, pathHex.R));
				turnCost = 0;
				return null;
			}

			pathHex = hexMap[pathHex];
		}

		// TODO: Better turn estimate, this is a bit too low over long distances
		turnCost = Mathf.CeilToInt(((float)hexG [targetHex] / (float)moveRange));
		Debug.Log (string.Format("Pathfinding: ~{0} turn path set!", turnCost));
		return new Queue<Hex>(path.Reverse());
	}

	/// <summary>
	/// Gets the move queue preview.
	/// </summary>
	/// <returns>The array of points which can be used to render a preview line.</returns>
	/// <param name="targetHex">Target hex.</param>
	public Vector3[] GetMoveQueuePreview (Hex targetHex, out int turnCost) {
		Queue<Hex> queue = FindPathToHex (targetHex, out turnCost);
		if (queue == null)
			return null;
		Hex[] path = queue.ToArray();
		Vector3[] result = new Vector3[path.Length+1];
		result[0] = currentHex.gameObject.transform.position + Vector3.up;
		for (int i = 0; i < path.Length; i++) { 
			Hex h = path[i];
			result[i+1] = h.gameObject.transform.position+Vector3.up;
		}
		return result;
	}

	/// <summary>
	/// Sets a path to the target hex.
	/// </summary>
	/// <param name="targetHex">Target hex.</param>
	public void SetPathToHex (Hex targetHex) {
		int turnCost;
		moveQueue = FindPathToHex (targetHex, out turnCost);
	}
		
	/// <summary>
	/// Does queued moves.
	/// </summary>
	public void DoTurn()
	{
		if (moveQueue.Peek ().occupyingUnit != null || (moveQueue.Peek ().belongsTo != belongsTo && moveQueue.Peek ().belongsTo != null)) {
			moveQueue = null;
			Debug.Log ("Path blocked");
			return;
		}

		if (moveQueue != null) {
			if (CIV_6_MOVE_RULES) {
				// Civ VI rules - Need enough movement to enter tile, this leaves unused movement points which I don't like it but I've implemented it anyway
				while (moveQueue.Count > 0 && movementCost [moveQueue.Peek ().elevationLevel] <= movesRemaining) {
					currentHex.occupyingUnit = null;
					currentHex = moveQueue.Dequeue ();
					currentHex.occupyingUnit = this;
					movesRemaining -= movementCost [currentHex.elevationLevel];
					gameObject.transform.parent = currentHex.gameObject.transform;
					gameObject.transform.localPosition= Vector3.zero;
				}
			} else {
				// Civ V rules - Can enter tiles even if not enough movement left, just need at least 1
				// NOTE: Pathfinding and turn estimation will not be as efficient or accurate in this mode 
				// since it always assumes entering the tile uses the exact amount of movement stated by the
				// type to movementCost lookup but this is not true since we do not need the full amount
				// just at least 1, I should probably fix this. We need to lookup the movement cost to cross
				// from one tile type to another, instead of just how much it costs to enter that tile type.
				// This will also be required for rivers and defensive walls etc.
				while (moveQueue.Count > 0 && movesRemaining > 0) {
					currentHex.occupyingUnit = null;
					currentHex = moveQueue.Dequeue ();
					currentHex.occupyingUnit = this;
					movesRemaining -= movementCost [currentHex.elevationLevel];
					gameObject.transform.parent = currentHex.gameObject.transform;
					gameObject.transform.localPosition = Vector3.zero;
				}
			}
			if (moveQueue.Count == 0)
				moveQueue = null;
		}
	}

	/// <summary>
	/// Checks to see if the unit needs orders
	/// </summary>
	/// <returns><c>true</c> if unit needs orders, <c>false</c> if not.</returns>
	public bool CheckIfNeedOrders () {
		return movesRemaining > 0 && moveQueue == null;
	}
}