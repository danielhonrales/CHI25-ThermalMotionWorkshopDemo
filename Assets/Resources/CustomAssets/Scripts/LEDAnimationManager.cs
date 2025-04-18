using UnityEngine;
using System.Collections;
using System.Linq;

public class LEDAnimationManager : MonoBehaviour
{
    public LEDNode[] ledNodes;

    public float duration;
    public float overlap;

    public float maxChargeIntensity;
    public float minChargeIntensity;
    public float maxDischargeIntensity;
    public float minDischargeIntensity;

    private float timeStep = 10;
    private float singleDuration;

    public Material fire;
    public Material ice;
    public Material neutral;
    public bool isFire;
    public bool isIce;

    public Color32 lightColor;
    public Color32 fireColor;
    public Color32 iceColor;
    
    public Material activeMaterial;

    public AudioSource absorbAudio;
    public AudioSource chargeAudio;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            
            StartCoroutine(PlayLights(0));
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            
            StartCoroutine(PlayLights(1));
        }

        if(isFire)
        {
            activeMaterial = fire;
            lightColor = fireColor;
        }
        
        if (isIce)
        {
            activeMaterial = ice;
            lightColor = iceColor;
        }
        
    }

    public void PlayChargeAnimation()
    {
        StartCoroutine(PlayLights(0));
    }

    public void PlayDischargeAnimation()
    {
        StartCoroutine(PlayLights(1));
    }


    public IEnumerator PlayLights(int mode)
    {
        float cap = duration / (ledNodes.Length - 1);
        overlap = Mathf.Min(overlap, cap);
        singleDuration = duration - (overlap * (ledNodes.Length - 1));
        Debug.Log(singleDuration);
        if (mode == 0)
        {
            Debug.Log("Mode 0");
            /*foreach (LEDNode ledNode in Array.Reverse(ledNodes))
            {

            }*/
            int j = ledNodes.Length - 1;
            while(j>=0)
            {
                Debug.Log("Charging");
                ledNodes[j].GetComponent<Renderer>().material = activeMaterial;
                ledNodes[j].pointLight.GetComponent<Renderer>().material.SetColor("_EmissionColor", lightColor);
                StartCoroutine(ChargeLight(ledNodes[j]));
                j--;
                yield return new WaitForSeconds((singleDuration - overlap) / 1000);
            }
        }
        else
        {
            for (int j = 0; j < ledNodes.Length; j++)
            {
                ledNodes[j].GetComponent<Renderer>().material = activeMaterial;
                StartCoroutine(DrainLight(ledNodes[j]));
                yield return new WaitForSeconds((singleDuration - overlap) / 1000);
            }
        }

        /*foreach (LEDNode ledNode in ledNodes)
        {
            if (mode == 0)
            {
                StartCoroutine(ChargeLight(ledNode));
            } else
            {
                StartCoroutine(DrainLight(ledNode));
            }
            yield return new WaitForSeconds((singleDuration - overlap) / 1000);
        }*/

        if (mode == 0) {
            absorbAudio.Stop();
        } else {
            yield return new WaitForSeconds(2f);
            chargeAudio.Stop();
        }
    }

    public IEnumerator ChargeLight(LEDNode ledNode)
    {
        
        for (int i = 0; i < singleDuration / timeStep; i++)
        {
            ledNode.pointLight.intensity += (maxChargeIntensity - minChargeIntensity) / (singleDuration / timeStep);
            yield return new WaitForSeconds(timeStep / 1000);
        }
    }

    public IEnumerator DrainLight(LEDNode ledNode)
    {
        ledNode.pointLight.intensity = maxDischargeIntensity;
        for (int i = 0; i < singleDuration / timeStep; i++)
        {
            
            ledNode.pointLight.intensity -= (maxDischargeIntensity - minDischargeIntensity) / (singleDuration / timeStep);
            yield return new WaitForSeconds(timeStep / 1000);
        }
    }
}
    // Method to start the animation sequence
   
