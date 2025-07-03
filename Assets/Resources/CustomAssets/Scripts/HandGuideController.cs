using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HandGuideController : MonoBehaviour
{

    public GameObject upGrab1;
    public GameObject upGrab2;
    public GameObject hold;
    public GameObject beam;
    public List<GameObject> handGuides;

    public TMP_Text holdTimer;
    
    private Quaternion originalRotation;
    public Coroutine currentGuide;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        handGuides = new List<GameObject> {
            upGrab1,
            upGrab2,
            hold,
            beam
        };
        originalRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (beam.activeSelf) {
            float angleX = Mathf.Sin(Time.time * .3f * 2 * Mathf.PI) * 30;
            float angleY = Mathf.Sin(Time.time * .3f * 2 * Mathf.PI + 90) * 30;
            Vector3 newRotation = new(
                originalRotation.eulerAngles.x + angleX,
                originalRotation.eulerAngles.y + angleY,
                transform.eulerAngles.z
            );
            transform.eulerAngles = newRotation;
        }
    }

    public void AllOff() {
        foreach (GameObject handGuide in handGuides) {
            handGuide.SetActive(false);
        }
        transform.rotation = originalRotation;
    }

    public IEnumerator GrabGuide() {
        if (currentGuide != null) StopCoroutine(currentGuide);
        AllOff();
        upGrab1.SetActive(true);

        while (true) {
            yield return new WaitForSeconds(1f);
            upGrab1.SetActive(!upGrab1.activeSelf);
            upGrab2.SetActive(!upGrab2.activeSelf);
        }
    }

    public IEnumerator HoldGuide() {
        if (currentGuide != null) StopCoroutine(currentGuide);
        AllOff();
        hold.SetActive(true);
        
        int timer = 5;
        while (timer > 0) {
            holdTimer.text = timer.ToString();
            yield return new WaitForSeconds(1f);
            timer--;
        }

        currentGuide = StartCoroutine(BeamGuide());
    }

    public IEnumerator BeamGuide() {
        if (currentGuide != null) StopCoroutine(currentGuide);
        AllOff();
        beam.SetActive(true);

        yield return new WaitForSeconds(5f);
        AllOff();
    }
}
