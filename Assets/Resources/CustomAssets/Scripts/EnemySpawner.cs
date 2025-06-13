using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    public List<GameObject> enemyPrefabs;
    public List<GameObject> enemyInstances;
    public Transform enemySpawnPoint;
    [Range(0, 30)]
    public int maxEnemyCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyInstances = new();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator SpawnEnemy() {
        yield return new WaitForSeconds(.5f);
        if (enemyInstances.Count < maxEnemyCount) {
            GameObject newEnemy = Instantiate(
                enemyPrefabs[Random.Range(0, enemyPrefabs.Count)],
                enemySpawnPoint.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f)),
                Quaternion.identity
            );
            enemyInstances.Add(newEnemy);
        }
        StartCoroutine(SpawnEnemy());
    }

    public void ClearEnemies() {
        foreach (GameObject enemy in enemyInstances) {
            Destroy(enemy);
        }
        enemyInstances = new();
    }
}
