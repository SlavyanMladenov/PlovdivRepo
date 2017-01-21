using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class EchoSphere
{
	public Texture2D EchoTexture;
	public Material EchoMaterial = null;
	public Vector3 Position;
	public int SphereIndex = 0;

	// Echo sphere Properties
	public float SphereMaxRadius = 10.0f;		//Final size of the echo sphere.
	private float sphereCurrentRadius = 0.0f;	//Current size of the echo sphere

	public float WaveLenght = 2.0f;			//Lenght of the wave
	public float FadeDelay = 0.0f;			//Time to delay before triggering fade.
	public float FadeRate = 1.0f;			//Speed of the fade away
	public float echoSpeed = 1.0f;			//Speed of the sphere growth.
	public bool is_manual = false;			//Is pulse manual.  if true, pulse triggered by left-mouse click

	private bool is_animated = false;		//If true, pulse is currently running.

	public float pulse_frequency = 5.0f;
	private float deltaTime = 0.0f;
	private float fade = 0.0f;

	public EchoSphere(){}

	// Update is called once per frame
	public void Update ()
	{
		if (EchoMaterial == null)
		{
			return;	
		}

		// If manual selection is disabled, automatically trigger a pulse at the given freq.
		deltaTime += Time.deltaTime;
		UpdateEcho();

		UpdateProperties();
	}

	// Called to trigger an echo pulse
	public void TriggerPulse()
	{
		deltaTime = 0.0f;
		sphereCurrentRadius = 0.0f;
		fade = 0.0f;
		is_animated = true;
	}

	// Called to halt an echo pulse.
	void HaltPulse()
	{
		//Debug.Log("HaltPulse reached");
		//is_animated = false;
		ClearPulse ();
	}

	public void ClearPulse()
	{
		fade = 0.0f;
		sphereCurrentRadius = 0.0f;
		is_animated = false;
	}

	void UpdateProperties()
	{
		if(!is_animated)return;
		float maxRadius = SphereMaxRadius;
		float maxFade = SphereMaxRadius / echoSpeed;

		//Debug.Log("Updating _Position"+SphereIndex.ToString());
		EchoMaterial.SetVector("_Position"+SphereIndex.ToString(),Position);
		EchoMaterial.SetFloat("_Radius"+SphereIndex.ToString(),sphereCurrentRadius);
		EchoMaterial.SetFloat("_Fade"+SphereIndex.ToString(),fade);

		EchoMaterial.SetFloat("_WaveLenght",WaveLenght);
		EchoMaterial.SetFloat("_MaxRadius",maxRadius);
		EchoMaterial.SetFloat("_MaxFade",maxFade);
	}

	// Called to update the echo front edge
	void UpdateEcho()
	{
		if(!is_animated)return;

		if(sphereCurrentRadius >= SphereMaxRadius)
		{
			HaltPulse();
		}
		else
		{
			sphereCurrentRadius += Time.deltaTime * echoSpeed;  
		}

		float radius = sphereCurrentRadius;
		float maxRadius = SphereMaxRadius;
		float maxFade = SphereMaxRadius / echoSpeed;
		if(fade > maxFade)
		{
			return;
		}

		if(deltaTime > FadeDelay)
			fade += Time.deltaTime * FadeRate;
	}

	public bool IsAnimated()
	{
		return is_animated;
	}
}