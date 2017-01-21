using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EchoSpheres : MonoBehaviour
{
	public Texture2D EchoTexture;
	public List<Material> EchoMaterials = null;
	
	public int SphereCount = 1;
	public int CurrentSphere = 0;
	
	// Echo sphere Properties
	public float SphereMaxRadius = 10.0f;		//Final size of the echo sphere.

	public float WaveLenght = 2.0f;			//Lenght of the wave
	public float FadeDelay = 0.0f;			//Time to delay before triggering fade.
	public float FadeRate = 1.0f;			//Speed of the fade away
	public float echoSpeed = 1.0f;			//Speed of the sphere growth.
	
	private List<EchoSphere[]> Spheres = new List<EchoSphere[]>();
		
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
			EchoSphere[] spheres = new EchoSphere[EchoMaterials.Count];
			for (int j = 0, m = EchoMaterials.Count; j < m; j++)
			{
				spheres[j] = new  EchoSphere {
					EchoMaterial = EchoMaterials[j],
					EchoTexture = EchoTexture,
					echoSpeed = echoSpeed,
					SphereMaxRadius = SphereMaxRadius,
					WaveLenght = WaveLenght,
					FadeDelay = FadeDelay,
					FadeRate = FadeRate,
					SphereIndex = i,
				};

				spheres [j].ClearPulse ();
			}

			Spheres.Add(spheres);
		}
	}
	/// <summary>
	/// Create an echo texture used to hold multiple echo sources and fades.
	/// </summary>
	void CreateEchoTexture()
	{
		for (int i = 0, n = EchoMaterials.Count; i < n; i++)
		{
			EchoTexture = new Texture2D (128, 128, TextureFormat.RGBA32, false);
			EchoTexture.filterMode = FilterMode.Point;
			EchoTexture.Apply ();
		
			EchoMaterials[i].SetTexture ("_EchoTex", EchoTexture);
		}
	}
	// Update is called once per frame
	void Update ()
	{
		if (EchoMaterials == null)
		{
			return;	
		}

		foreach (EchoSphere[] es in Spheres)
		{
			for (int i = 0, n = es.Length; i < n; i++)
			{
				es[i].Update();
			}
		}

		if (Input.GetKeyDown (KeyCode.Space))
		{
			//Debug.Log("Triggering pulse["+CurrentSphere.ToString()+"]");
			StartEcho(transform.position);
		}
	}

	public void StartEcho(Vector3 pos)
	{
		TriggerPulse();
		SetPositions(pos);

		CurrentSphere += 1;
		if(CurrentSphere >= Spheres.Count)CurrentSphere = 0;
	}

	void TriggerPulse()
	{
		foreach (EchoSphere[] es in Spheres)
		{
			for (int i = 0, n = es.Length; i < n; i++)
			{
				if(!es[i].IsAnimated())
					es[i].ClearPulse();
			}
		}

		for (int i = 0, n = Spheres [CurrentSphere].Length; i < n; i++)
		{
			Spheres[CurrentSphere][i].TriggerPulse();
		}
	}

	void SetPositions(Vector3 pos)
	{
		for (int i = 0, n = Spheres [CurrentSphere].Length; i < n; i++)
		{
			Spheres[CurrentSphere][i].Position = pos;
		}
	}
}
