using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitComponent : MonoBehaviour {

	public Mesh cubeModel, sphereModel, coneModel;
	public Unit unit;

	public void SetVisuals (int type) {
		switch (type) {
			case 0:
				GetComponentInChildren<MeshFilter> ().mesh = cubeModel;
				break;
			case 1:
				GetComponentInChildren<MeshFilter> ().mesh = sphereModel;
				break;
			case 2:
				GetComponentInChildren<MeshFilter> ().mesh = coneModel;
				break;
			default:
				break;
		}
	}
}
