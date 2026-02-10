using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VideoStuff : MonoBehaviour
{

    public EnemySpawner enemySpawner;
    public GameObject ledTube;
    public GameObject coldOrbPrefab;
    public GameObject hotOrbPrefab;
    public int tempType = 1;
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
            StartCoroutine(Attack(true));
        }

        if (Input.GetKey(KeyCode.Space) && Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(Absorb());
        }
    }

    public IEnumerator Attack(bool hit = false)
    {
        if (tempType % 2 == 0)
        {
            GameObject coldOrb = Instantiate(coldOrbPrefab);
            coldOrb.GetComponent<OrbController>().targetClientId.Value = 0;
            coldOrb.GetComponent<NetworkObject>().Spawn();
            coldOrb.GetComponent<SphereCollider>().enabled = false;

            GameObject randomEnemy = enemySpawner.enemyInstances[Random.Range(0, enemySpawner.enemyInstances.Count)];
            coldOrb.transform.position = randomEnemy.transform.position;

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

            if (hit)
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
        } else
        {
            GameObject hotOrb = Instantiate(hotOrbPrefab);
            hotOrb.GetComponent<OrbController>().targetClientId.Value = 0;
            hotOrb.GetComponent<NetworkObject>().Spawn();
            hotOrb.GetComponent<SphereCollider>().enabled = false;

            GameObject randomEnemy = enemySpawner.enemyInstances[Random.Range(0, enemySpawner.enemyInstances.Count)];
            hotOrb.transform.position = randomEnemy.transform.position;

            Vector3 end = ledTube.transform.position;

            float speed = 17f; // crank this up for FAST
            while (Vector3.Distance(hotOrb.transform.position, end) > 0.01f)
            {
                hotOrb.transform.position = Vector3.MoveTowards(
                    hotOrb.transform.position,
                    end,
                    speed * Time.deltaTime
                );

                yield return null; // update every frame
            }

            hotOrb.transform.position = end;

            transform.position = end;

            if (hit)
            effects.Find("HotHit").GetComponent<ParticleSystem>().Play();

            float duration = 0.3f;
            float elapsed = 0f;

            Vector3 startScale = hotOrb.transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Fast linear shrink
                hotOrb.transform.localScale = Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    t
                );

                yield return null;
            }

            // Ensure it's gone
            hotOrb.transform.localScale = Vector3.zero;

            Destroy(hotOrb);
        }
        tempType++;

        
    }

    public IEnumerator Absorb()
    {
        GameObject coldOrb = Instantiate(coldOrbPrefab);
        coldOrb.GetComponent<OrbController>().targetClientId.Value = 0;
        coldOrb.GetComponent<NetworkObject>().Spawn();
        coldOrb.GetComponent<SphereCollider>().enabled = true;

        GameObject randomEnemy = enemySpawner.enemyInstances[Random.Range(0, enemySpawner.enemyInstances.Count)];
        coldOrb.transform.position = randomEnemy.transform.position;

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
        float delayBetween = 0.4f;

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

