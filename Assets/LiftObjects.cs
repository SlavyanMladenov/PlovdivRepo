using UnityEngine;
using System.Collections;

public class LiftObjects : MonoBehaviour {



	public Camera object1;
	public GameObject object2;


	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void OnPointerEnter()
	{
		object1.transform.parent = object2.transform;

	}

	void Update () {
	
	}
}
