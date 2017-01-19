using UnityEngine;
using System.Collections;

public class Bullet :MonoBehaviour
{
	public float speed;
	public GameObject bullet;
	public Transform shotSpawn;
	public float thrust;

	private Rigidbody rb;
	void Start() 
	{
		rb = GetComponent<Rigidbody>();
	}

	/*void FixedUpdate() 
	{
		rb.AddForce(transform.forward * thrust);
	}*/

	public void OnShoot(){
		Instantiate(bullet, shotSpawn.position, shotSpawn.rotation);
		rb.AddForce(transform.forward * thrust);
	}
	void Update() {
		//OnShoot ();
	}
}