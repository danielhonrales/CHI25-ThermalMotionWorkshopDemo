using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    public Rigidbody rb;
    public ParticleSystem deathParticles;
    public AudioSource deathAudio;
    public AudioSource boopAudio;
    public AudioSource hoverAudio;
    public bool alive;
    public GameObject hotEnemyPrefab;
    public GameObject coldEnemyPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Boop());
        alive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) {
            boopAudio.Play();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (alive && other.gameObject.CompareTag("Beam")) {
            if (name.Contains("Hot") && other.transform.parent.GetComponent<BeamController>().isHotActive.Value) {
                StartCoroutine(Die());
            }
            if (name.Contains("Cold") && other.transform.parent.GetComponent<BeamController>().isColdActive.Value) {
                StartCoroutine(Die());
            }
        }
    }

    public IEnumerator Die() {
        alive = false;
        StopCoroutine(Boop());
        rb.useGravity = true;
        //rb.AddForce(Vector3.down * 10, ForceMode.Impulse);
        rb.AddTorque(Random.onUnitSphere * Random.Range(0f, 5f), ForceMode.Impulse);
        deathParticles.Play();
        deathAudio.Play();
        hoverAudio.Stop();

        yield return new WaitForSeconds(4);

        rb.useGravity = false;
        if (name.Contains("Hot")) {
            Instantiate(hotEnemyPrefab, GameObject.Find("HotEnemySpawnPoint").transform.position, Quaternion.Euler(0, -180, 0));
        } else {
            Instantiate(coldEnemyPrefab, GameObject.Find("ColdEnemySpawnPoint").transform.position, Quaternion.Euler(0, -180, 0));
        }
        Destroy(gameObject);
    }

    public IEnumerator Boop() {
        if (Random.Range(0, 3) == 0) {
            boopAudio.pitch = Random.Range(.5f, 2f);
            boopAudio.Play();
        }
        yield return new WaitForSeconds(1f);
        boopAudio.Stop();
        StartCoroutine(Boop());
    }
}
