using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines.Examples{
public class RollerCoasterLauch : MonoBehaviour {
    public RollerCoaster rollerCoaster;
    public DescriptionController description;
    public int[] descriptionTexts = new int[0];

	// Use this for initialization
	void Start () {
        StartCoroutine(Launch());
	}

    IEnumerator Launch()
    {
        rollerCoaster.AddBrake(10f);
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < descriptionTexts.Length; i++)
        {
            description.ShowText(descriptionTexts[i]);
            if(i < descriptionTexts.Length-1) yield return new WaitForSeconds(description.durations[descriptionTexts[i]]+1f);
        }
        GetComponent<Animator>().enabled = true;
        rollerCoaster.RemoveBrake();
        rollerCoaster.AddForce(20f);


    }
	// Update is called once per frame
	void Update () {
	
	}
}
}