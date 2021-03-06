using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraKeyboardControl : MonoBehaviour {

	public float moveSpeed = 250f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		Vector3 translate = new Vector3 (
			Input.GetAxis("Horizontal"),
		    0,
			Input.GetAxis("Vertical")
			);

		translate *= moveSpeed * Time.deltaTime;

		transform.Translate (translate*Time.deltaTime, Space.World);
	}
}
