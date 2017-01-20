using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class EchoSphere2
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
	
	public EchoSphere2(){}
	
	// Update is called once per frame
	public void Update ()
	{
		if(EchoMaterial == null)return;
		
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
		Debug.Log("HaltPulse reached");
		is_animated = false;	
	}
	
	void ClearPulse()
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
}
public class EchoSpheres : MonoBehaviour
{
	public Texture2D EchoTexture;
	public Material EchoMaterial = null;
	
	public int SphereCount = 1;
	public int CurrentSphere = 0;
	
	// Echo sphere Properties
	public float SphereMaxRadius = 10.0f;		//Final size of the echo sphere.

	public float WaveLenght = 2.0f;			//Lenght of the wave
	public float FadeDelay = 0.0f;			//Time to delay before triggering fade.
	public float FadeRate = 1.0f;			//Speed of the fade away
	public float echoSpeed = 1.0f;			//Speed of the sphere growth.
	
	private List<EchoSphere2> Spheres = new List<EchoSphere2>();
		
	// Use this for initialization
	void Start ()
	{		
		CreateEchoTexture();
		InitializeSpheres();
	}
	
	void InitializeSpheres()
	{
		for(int i = 0; i < SphereCount; i++)
		{
			EchoSphere2 es = new  EchoSphere2{
				EchoMaterial = EchoMaterial,
				EchoTexture = EchoTexture,
				echoSpeed = echoSpeed,
				SphereMaxRadius = SphereMaxRadius,
				WaveLenght = WaveLenght,
				FadeDelay = FadeDelay,
				FadeRate = FadeRate,
				SphereIndex = i,
			};
			Spheres.Add(es);
		}
	}
	/// <summary>
	/// Create an echo texture used to hold multiple echo sources and fades.
	/// </summary>
	void CreateEchoTexture()
	{
		EchoTexture = new Texture2D(128,128,TextureFormat.RGBA32,false);
		EchoTexture.filterMode = FilterMode.Point;
		EchoTexture.Apply();
		
		EchoMaterial.SetTexture("_EchoTex",EchoTexture);
	}
	// Update is called once per frame
	void Update ()
	{
		if(EchoMaterial == null)return;	

		foreach (EchoSphere2 es in Spheres)
		{
			es.Update();
		}

		if (Input.GetKeyDown (KeyCode.Space))
		{
			Debug.Log("Triggering pulse["+CurrentSphere.ToString()+"]");
			Spheres[CurrentSphere].TriggerPulse();
			Spheres[CurrentSphere].Position = transform.position;

			CurrentSphere += 1;
			if(CurrentSphere >= Spheres.Count)CurrentSphere = 0;
		}

		UpdateRayCast();
	}

	// Called to manually place echo pulse
	void UpdateRayCast()
	{
		if (Input.GetButtonDown("Fire1"))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		  	RaycastHit hit;
	        if (Physics.Raycast(ray,out hit, 10000))
			{
	            Debug.Log("Triggering pulse["+CurrentSphere.ToString()+"]");
				Spheres[CurrentSphere].TriggerPulse();
				Spheres[CurrentSphere].Position = hit.point;
				
				CurrentSphere += 1;
				if(CurrentSphere >= Spheres.Count)CurrentSphere = 0;
			}
		}
	}
}
