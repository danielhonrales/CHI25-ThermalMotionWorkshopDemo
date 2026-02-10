using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VideoStuff : MonoBehaviour
{

    public GameObject enemy;
    public GameObject ledTube;
    public GameObject coldOrbPrefab;
    public Transform effects;
    public List<GameObject> shootRings;

    public Transform ledTubeContainer;
    public Transform avatarContainer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ledTube = ledTubeContainer.Find("ledTube 0").gameObject;
            effects = ledTube.transform.Find("Effects");
        }
        if (Input.GetKey(KeyCode.Space) && Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(Attack());
        }

        if (Input.GetKey(KeyCode.Space) && Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(Absorb());
        }
    }

    public IEnumerator Attack()
    {
        GameObject coldOrb = Instantiate(coldOrbPrefab);
        coldOrb.GetComponent<OrbController>().targetClientId.Value = 0;
        coldOrb.GetComponent<NetworkObject>().Spawn();
        coldOrb.GetComponent<SphereCollider>().enabled = false;

        coldOrb.transform.position = enemy.transform.position;

        Vector3 end = ledTube.transform.position;

        float speed = 17f; // crank this up for FAST
        while (Vector3.Distance(coldOrb.transform.position, end) > 0.01f)
        {
            coldOrb.transform.position = Vector3.MoveTowards(
                coldOrb.transform.position,
                end,
                speed * Time.deltaTime
            );

            yield return null; // update every frame
        }

        coldOrb.transform.position = end;

        transform.position = end;

        effects.Find("ColdHit").GetComponent<ParticleSystem>().Play();

        float duration = 0.3f;
        float elapsed = 0f;

        Vector3 startScale = coldOrb.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Fast linear shrink
            coldOrb.transform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                t
            );

            yield return null;
        }

        // Ensure it's gone
        coldOrb.transform.localScale = Vector3.zero;

        Destroy(coldOrb);
    }

    public IEnumerator Absorb()
    {
        GameObject coldOrb = Instantiate(coldOrbPrefab);
        coldOrb.GetComponent<OrbController>().targetClientId.Value = 0;
        coldOrb.GetComponent<NetworkObject>().Spawn();
        coldOrb.GetComponent<SphereCollider>().enabled = true;

        coldOrb.transform.position = enemy.transform.position;

        Vector3 end = avatarContainer.Find("LocalAvatar").Find("Joint RightHandWrist").position;

        float speed = 12f; // crank this up for FAST
        while (Vector3.Distance(coldOrb.transform.position, end) > 0.01f)
        {
            coldOrb.transform.position = Vector3.MoveTowards(
                coldOrb.transform.position,
                end,
                speed * Time.deltaTime
            );

            yield return null; // update every frame
        }

        coldOrb.transform.position = end;

        transform.position = end;

        GameObject iceWhirl = effects.Find("Absorb").Find("Snow slash").gameObject;
        iceWhirl.GetComponent<ParticleSystem>().Play();

        float distance = 3f;
        float duration = 4f;

        Vector3 startPos = iceWhirl.transform.localPosition;                  // Use localPosition
        Vector3 endPos = startPos + new Vector3(distance, 0f, 0f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Linear interpolation in local space
            iceWhirl.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        // Ensure final local position
        iceWhirl.transform.localPosition = endPos;

        iceWhirl.GetComponent<ParticleSystem>().Stop();

        yield return new WaitForSeconds(2f);

        shootRings.Add(effects.Find("Shoot").Find("5").gameObject);
        shootRings.Add(effects.Find("Shoot").Find("4").gameObject);
        shootRings.Add(effects.Find("Shoot").Find("3").gameObject);
        shootRings.Add(effects.Find("Shoot").Find("2").gameObject);
        shootRings.Add(effects.Find("Shoot").Find("1").gameObject);

        int count = shootRings.Count;
        int index = 0;
        int currentLoop = 0;
        int loops = 3;
        int maxActive = 2;
        float delayBetween = 0.5f;

        while (currentLoop < loops)
        {
            // Activate current ring
            shootRings[index].GetComponent<ParticleSystem>().Play();

            // Deactivate the ring that is "maxActive" behind
            int oldIndex = index - maxActive;
            if (oldIndex >= 0)
            {
                shootRings[oldIndex].GetComponent<ParticleSystem>().Stop();
            }
            else if (oldIndex < 0)
            {
                int wrapIndex = count + oldIndex;
                if (wrapIndex >= 0)
                    shootRings[wrapIndex].GetComponent<ParticleSystem>().Stop();
            }

            // Move to next ring
            index++;

            // Completed a full loop
            if (index >= count)
            {
                index = 0;
                currentLoop++;
            }

            yield return new WaitForSeconds(delayBetween);
        }
        foreach (var ring in shootRings)
            ring.GetComponent<ParticleSystem>().Stop();
    }
}

