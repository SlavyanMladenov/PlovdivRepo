using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieController : MonoBehaviour
{
	private bool playerInRange = false;
	private Animator animator;

	void Awake()
	{
		animator = GetComponent<Animator> ();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag ("Player"))
		{
			animator.SetBool ("Attack", true);
		}
	}
}
