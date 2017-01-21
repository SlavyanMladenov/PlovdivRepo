using UnityEngine;
using System.Collections;

public class LookTouchWalk : MonoBehaviour
{
	public Transform VrCamera;
	public AudioSource stepAudioSource;
	public AudioClip footstepClip;
	public EchoSpheres footStepsEcho;
	public float stepGapDuration = 0.5f;
	public float ToggleAngle = 30.0f;
	public float speed = 3.0f;
	private bool moveForward;
	private CharacterController cc;

	private float stepTimer = 0f;

	// Use this for initialization
	void Start ()
	{
		cc = GetComponent<CharacterController> ();
	}

	// Update is called once per frame
	void Update ()
	{
		if (Input.touchCount > 0 && Input.GetTouch (0).phase == TouchPhase.Stationary || Input.GetKey(KeyCode.W))
		{
			moveForward = true;
		}
		else
		{
			moveForward = false;
			stepTimer = 0f;
		}

		if (moveForward)
		{
			Vector3 forward = VrCamera.TransformDirection (Vector3.forward);
			cc.SimpleMove (forward * speed);

			UpdateStep ();
		}
	}

	void UpdateStep()
	{
		if (stepTimer <= 0f)
		{
			footStepsEcho.StartEcho (stepAudioSource.transform.position);
			stepAudioSource.PlayOneShot(footstepClip);
			stepTimer = stepGapDuration;
		}

		stepTimer -= Time.deltaTime;
	}
}