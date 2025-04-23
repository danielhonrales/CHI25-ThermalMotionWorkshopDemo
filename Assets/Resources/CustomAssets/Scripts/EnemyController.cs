using System;
using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    public Rigidbody rb;
    public ParticleSystem deathParticles;
    public AudioSource deathAudio;
    public AudioSource boopAudio;
    public AudioSource hoverAudio;
    public EnemyState state;
    public GameObject hotEnemyPrefab;
    public GameObject coldEnemyPrefab;

    public Vector3 targetPoint;
    public GameObject justSpawnedTargetArea;
    public GameObject homingInTargetArea;
    public float moveSpeed;
    public float wiggleAmount;
    public float wiggleSpeed;
    public float separationDistance;
    public float separationWeight;
    public float minAltitude;
    public float boostForce;
    public float bobAmplitude = 0.5f;     // Max distance to move up/down
    public float bobFrequency = 1f;       // Speed of the bobbing
    private float seed;
    private float idleY;
    private GameObject player;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Boop());
        justSpawnedTargetArea = GameObject.Find("JustSpawnedTargetArea");
        homingInTargetArea = GameObject.Find("HomingInTargetArea");
        targetPoint = GetRandomPointInsideCube(justSpawnedTargetArea);
        state = EnemyState.justSpawned;
        seed = UnityEngine.Random.Range(0f, 100f);
        moveSpeed = 7.5f + UnityEngine.Random.Range(-1f, 1f);
        wiggleAmount = 1;
        wiggleSpeed = 1;
        separationDistance = 0f;
        separationWeight = 0f;
        minAltitude = targetPoint.y;
        boostForce = 30f + UnityEngine.Random.Range(-5f, 5f);
        bobAmplitude = 0.5f;
        bobFrequency = 1f;

    float randomScaleChange = UnityEngine.Random.Range(.3f, 1.7f);
        transform.localScale *= randomScaleChange;

        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) {
            boopAudio.Play();
        }

        if (Vector3.Distance(transform.position, targetPoint) < .1f) {
            if (state == EnemyState.justSpawned) {
                state = EnemyState.homingIn;
                targetPoint = GetRandomPointInsideCube(homingInTargetArea);
            } else if (state == EnemyState.homingIn) {
                state = EnemyState.hostileIdle;
                idleY = transform.position.y;
                player = GameObject.Find("LocalAvatar");
            }
        }
        
        if (state == EnemyState.justSpawned || state == EnemyState.homingIn) {
            Vector3 toPlayer = (targetPoint - transform.position).normalized;
            float time = Time.time * wiggleSpeed + seed;

            Vector3 wiggle = new Vector3(
                Mathf.PerlinNoise(time, 0f),
                Mathf.PerlinNoise(0f, time),
                Mathf.PerlinNoise(time, time)
            ) - Vector3.one * 0.5f;

            wiggle *= wiggleAmount;

            Vector3 separation = GetSeparation();

            Vector3 finalDir = (toPlayer + wiggle + separation * separationWeight).normalized;
            float newMoveSpeed = Math.Min(7.5f, moveSpeed *  Vector3.Distance(transform.position, targetPoint));
            rb.MovePosition(rb.position + newMoveSpeed * Time.deltaTime * finalDir);

            if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(finalDir)) > 5) {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(finalDir),
                    90 * Time.deltaTime
                );
            }
        }

        if (state == EnemyState.hostileIdle)
        {
            Vector3 finalDir = (player.transform.position - transform.position).normalized;
            if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(finalDir)) > 5)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(finalDir),
                    90 * Time.deltaTime
                );
            }
            float targetY = idleY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            float newX = Mathf.MoveTowards(transform.position.x, targetPoint.x, 1.5f * Time.deltaTime);
            float newY = Mathf.MoveTowards(transform.position.y, targetY, 1.5f * Time.deltaTime);
            float newZ = Mathf.MoveTowards(transform.position.z, targetPoint.z, 1.5f * Time.deltaTime);

            transform.position = new Vector3(newX, newY, newZ);
        }

            /* if (transform.position.y < minAltitude)
            {
                rb.AddForce(Vector3.up * boostForce, ForceMode.Acceleration);
            } */
        }

    Vector3 GetSeparation()
    {
        Vector3 sep = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, separationDistance);

        foreach (var other in neighbors)
        {
            if (other.transform == transform) continue;

            Vector3 away = transform.position - other.transform.position;
            float dist = away.magnitude;
            if (dist > 0.01f && dist < separationDistance) {
                sep += away.normalized / dist;
            }
        }

        return sep;
    }

    void OnTriggerEnter(Collider other)
    {
        if (state != EnemyState.dead && other.gameObject.CompareTag("Beam")) {
            GameController gameController = GameObject.Find("GameController").GetComponent<GameController>();
            if (name.Contains("Hot") && other.transform.parent.GetComponent<BeamController>().isHotActive.Value) {
                gameController.hotKills++;
            }
            if (name.Contains("Cold") && other.transform.parent.GetComponent<BeamController>().isColdActive.Value) {
                gameController.coldKills++;
            }
            StartCoroutine(Die());
        }
    }

    public IEnumerator Die() {
        state = EnemyState.dead;
        StopCoroutine(Boop());
        rb.useGravity = true;
        //rb.AddForce(Vector3.down * 10, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0f, 5f), ForceMode.Impulse);
        deathParticles.Play();
        deathAudio.Play();
        hoverAudio.Stop();

        yield return new WaitForSeconds(4);

        rb.useGravity = false;
        GameObject.Find("EnemySpawner").GetComponent<EnemySpawner>().enemyInstances.Remove(gameObject);
        Destroy(gameObject);
    }

    public IEnumerator Boop() {
        if (UnityEngine.Random.Range(0, 5) == 0) {
            boopAudio.pitch = UnityEngine.Random.Range(.5f, 2f);
            boopAudio.Play();
        }
        yield return new WaitForSeconds(1f);
        boopAudio.Stop();
        StartCoroutine(Boop());
    }

    public static Vector3 GetRandomPointInsideCube(GameObject cube)
    {
        Vector3 center = cube.transform.position;
        Vector3 size = cube.transform.localScale;

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-0.5f, 0.5f) * size.x,
            UnityEngine.Random.Range(-0.5f, 0.5f) * size.y,
            UnityEngine.Random.Range(-0.5f, 0.5f) * size.z
        );

        return center + cube.transform.rotation * randomOffset;
    }

    public enum EnemyState
    {
        justSpawned,
        homingIn,
        hostileIdle,
        dead
    }
}
