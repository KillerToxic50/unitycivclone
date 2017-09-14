using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

	public GameObject hexMap;
	public Button actionButton;
	public GameObject infoPanel;
	public Text infoTitle;
	public Text info1;
	public Text info2;
	public Unit selectedUnit;
	public City selectedCity;

	void Start () {
		actionButton.GetComponent<Button>().onClick.AddListener(OnActionButton);
	}

	void Update () {
		Unit u = hexMap.GetComponent<HexMap>().GetUnitThatNeedOrders();
		if (u == null) {
			actionButton.GetComponentInChildren<Text> ().text = "NEXT TURN";
		} else {
			actionButton.GetComponentInChildren<Text> ().text = "A UNIT NEEDS ORDERS";
		}


		if (selectedUnit != null || selectedCity != null) {
			infoPanel.SetActive (true);
		} else {
			infoPanel.SetActive(false);
		}

		if (selectedUnit != null) {
			// Update info panel with unit info
			infoTitle.text = "UNIT INFO:";
			info1.text = string.Format ("Movement: {0}/{1}", Mathf.Clamp(selectedUnit.movesRemaining, 0,selectedUnit.moveRange), selectedUnit.moveRange);
			info2.text = string.Format ("Health: {0}/{1}", 100, 100);
		}

		if (selectedCity != null) {
			// Update info panel with city info
			infoTitle.text = "CITY INFO:";
			info1.text = string.Format ("Production: {0}/{1}", selectedCity.production, selectedCity.currentProductionCost);
			info2.text = string.Format ("ProductionPerTurn: {0}", selectedCity.productionPerTurn);
		}
	}

	void OnActionButton () {
		// TODO: Check if cities need production assigned
		Unit u = hexMap.GetComponent<HexMap>().GetUnitThatNeedOrders();
		if (u == null) {
			Debug.Log ("UIManager: Starting next turn!");
			hexMap.GetComponent<HexMap>().ResetUnitMoves();
			hexMap.GetComponent<HexMap>().DoUnitTurns();
			hexMap.GetComponent<HexMap>().DoCityTurns();
		} else {
			Debug.Log ("UIManager: Selecting unit that still needs orders!");
			Camera.main.GetComponent<CameraMouseControl>().SelectUnit(u);
			Camera.main.GetComponent<CameraMouseControl>().PanCameraToSelectedUnit();
		}
	}
}
