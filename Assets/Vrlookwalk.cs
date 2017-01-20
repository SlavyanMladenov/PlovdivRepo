using UnityEngine;
using System.Collections;

public class Vrlookwalk : MonoBehaviour {

	public Transform VrCamera;

	public float ToggleAngle = 30.0f;

	public float speed = 3.0f;

	private bool moveForward;

	private CharacterController cc;

	// Use this for initialization
	void Start () {
		cc = GetComponent<CharacterController> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (VrCamera.eulerAngles.x >= ToggleAngle && VrCamera.eulerAngles.x < 90.0f)
			moveForward = true;
		else {
			moveForward = false;
		}

		if (moveForward) {
			Vector3 forward = VrCamera.TransformDirection (Vector3.forward);

			cc.SimpleMove (forward * speed);
		}
	
	}
}
