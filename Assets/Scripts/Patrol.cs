using UnityEngine;
using System.Collections.Generic;

public class Patrol : MonoBehaviour
{
	public GameObject target;
	public Transform patrolPointsParent;
	private List<Transform> patrolPoints;
	public float speed = 1f;
    
	private int patrolIndex = 0;
	private float threshold = 0.5f;

	private bool active = false;

	void Start()
	{
		foreach (Transform t in patrolPointsParent)
		{
			patrolPoints.Add (t);
		}

		patrolPointsParent.DetachChildren ();
		transform.position = patrolPoints [patrolIndex].position;
		active = true;
	}

	public void Chase()
	{
		active = false;


	}

	void Update()
	{
		if (!active)
		{
			return;
		}

        Vector3 nextPos = patrolPoints[patrolIndex + 1].position;
        transform.position += (nextPos - transform.position).normalized * speed;

        if(Vector3.Distance(transform.position, nextPos) <= threshold)
        {
            patrolIndex++;

            if(patrolIndex >= patrolPoints.Count)
            {
                patrolIndex = 0;
            }
        }
    }
}
