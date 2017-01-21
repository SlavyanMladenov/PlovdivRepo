using UnityEngine;
using System.Collections;


public class NoteFinder : MonoBehaviour
{
    public GameObject audioInputObject;

    public float threshold = 1.0f;
    MicrophoneInput micIn;
	public AudioSource audio;
    // Use this for initialization
    void Start()
    {
       
        micIn = (MicrophoneInput)audioInputObject.GetComponent("MicrophoneInput");
    }

    // Update is called once per frame
    void Update()
    {
		float t = micIn.loudness;//Get the loudness from our MicrophoneInput script
		int f = (int)micIn.frequency;// Get the frequency from our MicrophoneInput script
		if (t>=0.8) // Compare the frequency to known value, take possible rounding error in to account
        {
			audio.Play();


            Debug.Log("WORKING!");
        }
        
    }
}
