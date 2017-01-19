using UnityEngine;
using System.Collections;

public class Flying : MonoBehaviour {

	public Transform VrCamera;

	public int ToggleAngle = 90;

	public float flyspeed = 3.0f;

	private bool fly;

	private CharacterController cc;

	// Use this for initialization
	void Start () {
		cc = GetComponent<CharacterController> ();
	}
	void SetTransformX(float n){
		transform.position = new Vector3(transform.position.x,n,transform.position.z);
	}
	// Update is called once per frame
	void Update () {
		//if (VrCamera.eulerAngles.x <= ToggleAngle)
		if (VrCamera.eulerAngles.x >=ToggleAngle)	
		fly = true;
		else {
			fly = false;
		}

		if (fly) {

			SetTransformX (flyspeed);
			flyspeed++;
		}

	}
}
