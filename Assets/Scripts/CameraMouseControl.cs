using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMouseControl : MonoBehaviour {

	public bool mobile = false;

	public float maxZoom = 5f;
	public float minZoom = 15f;

	public Material invalidPathMat, validPathMat;

	public HexMap hexMap;
	public UIManager uiManager;

	Vector3 currentMousePosition;
	Vector3 lastMousePosition;

	Ray mouseRay;
	RaycastHit rayHit;

	delegate void CurrentUpdateFunc();
	CurrentUpdateFunc func;

	Unit selectedUnit;
	City selectedCity;
	Hex lastHex;

	LineRenderer lr;

	// Use this for initialization
	void Start () {
		func = UpdateSelectAction;

		lr = GetComponent<LineRenderer> ();

		Ray mouseRay = Camera.main.ScreenPointToRay (Input.mousePosition);
		float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
		lastMousePosition = mouseRay.origin - (mouseRay.direction * rayLength);
	}
	
	// Update is called once per frame
	void Update () {
		// Update current mouse position
		if (mobile) {
			if (Input.touchCount > 0) {
				Vector2 pointerPos = Input.GetTouch (0).position;
				mouseRay = Camera.main.ScreenPointToRay (pointerPos);
				float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
				currentMousePosition = mouseRay.origin - (mouseRay.direction * rayLength);
			}
		} else {
			Vector2 pointerPos = Input.mousePosition;
			mouseRay = Camera.main.ScreenPointToRay (pointerPos);
			float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
			currentMousePosition = mouseRay.origin - (mouseRay.direction * rayLength);
		}

		// Call the currently selected update function
		func();

		// Do scrolling
		DoZooming();

		// Update last mouse position
		if (mobile) {
			if (Input.touchCount > 0) {
				Vector2 pointerPos = Input.GetTouch (0).position;
				mouseRay = Camera.main.ScreenPointToRay (pointerPos);
				float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
				currentMousePosition = mouseRay.origin - (mouseRay.direction * rayLength);
			}
		} else {
			Vector2 pointerPos = Input.mousePosition;
			mouseRay = Camera.main.ScreenPointToRay (pointerPos);
			float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
			currentMousePosition = mouseRay.origin - (mouseRay.direction * rayLength);
		}
	}

	// I might change the camera angle to 45 degrees just to make the math a bit simpler
	public void PanCameraToSelectedUnit () {
		Vector3 p = selectedUnit.currentHex.gameObject.transform.position;
		p.y = Camera.main.transform.position.y;
		p.z += p.y / Mathf.Sin(30f);
		p.z += p.y / 5f;
		Camera.main.transform.position = p;
	}

	void UpdateSelectAction () {
		if (Input.GetMouseButtonDown (0)) {
			if (Physics.Raycast (mouseRay, out rayHit)) {
				switch (rayHit.collider.tag) {
				case "Unit":
					SelectUnit (rayHit.transform.GetComponentInParent<UnitComponent> ().unit);
					if (mobile) {
						func = UpdateMoveUnit;
						UpdateMoveUnit ();
					}
					break;
				case "Tile":
					func = UpdateCameraDrag;
					UpdateCameraDrag ();
					break;
				case "City":
					SelectCity (rayHit.transform.gameObject.GetComponent<CityComponent>().city);
					break;
				default:
					Debug.Log (string.Format ("SelectAction: Player clicked a {1}!", rayHit.collider.tag));
					break;
				}
			}
			else {
				//Debug.LogError ("SelectAction: Player clicked on nothing, something has gone very wrong!");
			}
		}

		if (!mobile) {
			if (Input.GetMouseButtonDown (1)) {
				func = UpdateMoveUnit;
				UpdateMoveUnit ();
			}

			if (Input.GetMouseButtonDown (2)) {
				Physics.Raycast (mouseRay, out rayHit);
				if (rayHit.transform.tag == "Tile") {
					Hex hitHex = rayHit.transform.GetComponentInParent<HexComponent> ().hex;
					hexMap.SpawnCity (hitHex);
				}
			}
		}
	}

	void ResetFunc () {
		func = UpdateSelectAction;
	}

	void UpdateCameraDrag () {
		if (Input.GetMouseButtonUp (0)) {
			ResetFunc();
		}
		Vector3 diff = Vector3.zero;
		if (mobile) {
			if (Input.touchCount == 1) {
				diff = new Vector3 (-Input.GetTouch (0).deltaPosition.x / 10f, 0, -Input.GetTouch (0).deltaPosition.y / 10f);
			}
		} else {	
			diff = lastMousePosition - currentMousePosition;
		}
		transform.Translate (diff, Space.World);
	}

	void UpdateMoveUnit () {
		Hex hitHex = null;
		if (selectedUnit != null) {
			Physics.Raycast (mouseRay, out rayHit);
			if (rayHit.transform.tag == "Tile") {
				hitHex = rayHit.transform.GetComponentInParent<HexComponent> ().hex;
				if (hitHex != lastHex) {
					int turnCost;
					Vector3[] pathPreview = selectedUnit.GetMoveQueuePreview (hitHex, out turnCost);
					if (pathPreview == null) {
						Debug.Log (string.Format ("MoveUnit: Previewing a ~{0} turn move!", turnCost));
						lr.material = invalidPathMat;
						lr.positionCount = 2;
						lr.SetPosition (0, selectedUnit.currentHex.gameObject.transform.position+Vector3.up);
						lr.SetPosition (1, hitHex.gameObject.transform.position+Vector3.up);
					} else {
						lr.material = validPathMat;
						lr.positionCount = pathPreview.Length;
						lr.SetPositions (pathPreview);
					}
					lr.enabled = true;
					lastHex = hitHex;
				}
			} else {
				Debug.Log ("MoveUnit: Invalid move location!");
			}
		} else {
			Debug.Log ("MoveUnit: No unit selected!");
		}

		if (mobile) {
			if (Input.GetMouseButtonUp (0)) {
				if (hitHex != null && selectedUnit != null) {
					selectedUnit.SetPathToHex (hitHex);
					selectedUnit.DoTurn ();
					Debug.Log ("MoveUnit: Move done!");
				}
				lr.enabled = false;
				ResetFunc ();
			}
		} else {
			if (Input.GetMouseButtonUp (1)) {
				if (hitHex != null && selectedUnit != null) {
					selectedUnit.SetPathToHex (hitHex);
					selectedUnit.DoTurn ();
					Debug.Log ("MoveUnit: Move done!");
				}
				lr.enabled = false;
				ResetFunc ();
			}
		}
	}

	public void SelectUnit (Unit unit) {
		Debug.Log ("UnitSelecter: Player selected a unit!");
		selectedUnit = unit;
		selectedCity = null;
		uiManager.selectedUnit = unit;
		uiManager.selectedCity = null;
	}

	void DeselectUnit () {
		Debug.Log ("UnitSelecter: Unit deselected!");
		selectedUnit = null;
		uiManager.selectedUnit = null;
	}

	void SelectCity (City city) {
		Debug.Log ("CitySelecter: Player selected a city!");
		selectedCity = city;
		selectedUnit = null;
		uiManager.selectedCity = city;
		uiManager.selectedUnit = null;
	}

	float lastScrollAmount = 0f;
	bool scrolling = false;

	void DoZooming () {
		float scrollAmount = 0f;
		if (mobile) {
			if (Input.touchCount == 2) {
				if (scrolling) {
					scrollAmount = (Vector2.Distance (Input.GetTouch (0).position, Input.GetTouch (1).position) / 10f) - lastScrollAmount;
				} else {
					lastScrollAmount = (Vector2.Distance (Input.GetTouch (0).position, Input.GetTouch (1).position) / 10f);
				}
				scrolling = true;
			}
			else {
				scrolling = false;
			}
		} else {
			scrollAmount = Input.GetAxis ("Mouse ScrollWheel");
		}
		if (Mathf.Abs (scrollAmount) > 0.01f) {
			Vector3 p = transform.position;
			Vector3 dir = currentMousePosition - p;

			if (p.y < minZoom) {
				if (scrollAmount < 0f) {
					transform.Translate (dir * scrollAmount, Space.World);
				} else {
					transform.Translate (-dir, Space.World);
				}
			}

			if (scrollAmount > 0f) {
				if (p.y > maxZoom) {
					transform.Translate (dir * scrollAmount, Space.World);
				} else {
					transform.Translate (-dir, Space.World);
				}
			}

			p = transform.position;
			if (p.y > minZoom) {
				p.y = minZoom;
			}
			else if (p.y > maxZoom) {
				p.y = maxZoom;
			}
			transform.position = p;
		}
	}
}
