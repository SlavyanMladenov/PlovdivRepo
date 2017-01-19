using UnityEngine;
using System.Collections;

public class SetPar : MonoBehaviour {

	private GameObject CameraRight;
	private GameObject CameraLeft;



	public Transform Parent;
	// Use this for initialization
	// Update is called once per frame
	void Update()
	{
			CameraLeft = GameObject.Find ("Main Camera Left");
			CameraRight = GameObject.Find ("Main Camera Right");
			CameraLeft.transform.SetParent (Parent);
			CameraRight.transform.SetParent (Parent);


	}





}
