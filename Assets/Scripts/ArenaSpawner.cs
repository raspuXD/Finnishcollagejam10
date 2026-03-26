using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArenaSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Enemy Prefabs")]
    public GameObject enemyNoPolarityPrefab;
    public GameObject enemyAttractPrefab;
    public GameObject enemyRepelPrefab;

    [System.Serializable]
    public class PropVariant
    {
        public string name;
        public GameObject noPolarityPrefab;
        public GameObject attractPrefab;
        public GameObject repelPrefab;

        public bool MetallicObject;

        [Range(0f, 1f)] public float spawnWeight = 1f; // relative chance of this variant being picked
    }

    [Header("Prop Variants")]
    public List<PropVariant> propVariants = new List<PropVariant>();

    [Header("Spawn Area")]
    public float spawnRadius = 18f;
    public float minSpawnDistance = 8f;
    public float spawnHeight = 0.5f;
    public LayerMask groundLayer;
    public float groundRaycastHeight = 10f;

    [Header("Spawn Timing")]
    public float baseEnemyInterval = 4f;
    public float minEnemyInterval = 1.2f;
    public float propInterval = 12f;

    [Header("Spawn Caps")]
    public int maxEnemies = 75;
    public int maxProps = 30;

    [Header("Difficulty Scaling")]
    public float difficultyRampTime = 120f;
    public float strengthMin = 500f;
    public float strengthMax = 2000f;
    public float rangeMin = 6f;
    public float rangeMax = 18f;

    [Header("Polarity Chances")]
    [Range(0f, 1f)] public float noPolarityChance = 0.35f;
    [Range(0f, 1f)] public float attractChance = 0.35f;

    private float difficultyT => Mathf.Clamp01(Time.time / difficultyRampTime);

    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<GameObject> activeProps = new List<GameObject>();

    void Start()
    {
        StartCoroutine(EnemySpawnLoop());
        StartCoroutine(PropSpawnLoop());
    }

    // ─── Spawn Loops ────────────────────────────────────────────────

    IEnumerator EnemySpawnLoop()
    {
        while (true)
        {
            float interval = Mathf.Lerp(baseEnemyInterval, minEnemyInterval, difficultyT);
            yield return new WaitForSeconds(interval);

            CleanDestroyedObjects(activeEnemies);

            if (activeEnemies.Count < maxEnemies)
                SpawnEnemy();
        }
    }

    IEnumerator PropSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(propInterval);

            CleanDestroyedObjects(activeProps);

            if (activeProps.Count < maxProps)
                SpawnProp();
        }
    }

    // ─── Enemy Spawning ──────────────────────────────────────────────

    void SpawnEnemy()
    {
        Vector3? spawnPos = GetSpawnPoint();
        if (spawnPos == null) return;

        float roll = Random.value;
        GameObject prefab;
        bool usePolarity;
        MagnetPolarity polarity = MagnetPolarity.Attract;

    

        if (roll < noPolarityChance)
        {
            prefab = enemyNoPolarityPrefab;
            usePolarity = false;
        }
        else if (roll < noPolarityChance + attractChance)
        {
            prefab = enemyAttractPrefab;
            usePolarity = true;
            polarity = MagnetPolarity.Attract;
        }
        else
        {
            prefab = enemyRepelPrefab;
            usePolarity = true;
            polarity = MagnetPolarity.Repel;
        }

        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, spawnPos.Value, Quaternion.identity);

        MagneticEnemyController controller = enemy.GetComponent<MagneticEnemyController>();
        if (controller != null)
            controller.player = player;

        MetalObject metal = enemy.GetComponent<MetalObject>();
        if (metal != null)
        {
            metal.usePolarity = usePolarity;
            metal.polarity = polarity;
            metal.objectType = MetalObject.ObjectType.Enemy;

            metal.magneticStrength = Mathf.Lerp(strengthMin, strengthMax, difficultyT)
                                     + Random.Range(-200f, 200f);
            metal.range = Mathf.Lerp(rangeMin, rangeMax, difficultyT)
                          + Random.Range(-1f, 1f);

            metal.magneticStrength = Mathf.Max(metal.magneticStrength, 100f);
            metal.range = Mathf.Max(metal.range, 3f);
        }

        activeEnemies.Add(enemy);
    }

    // ─── Prop Spawning ───────────────────────────────────────────────

    void SpawnProp()
    {
        if (propVariants == null || propVariants.Count == 0) return;

        Vector3? spawnPos = GetSpawnPoint();
        if (spawnPos == null) return;

        PropVariant variant = PickPropVariant();
        if (variant == null) return;

        // Pick polarity and matching prefab
        float roll = Random.value;
        GameObject prefab;
        MagnetPolarity polarity;
        bool SKIGITY;


        if(SKIGITY = variant.MetallicObject)
        {
            prefab = variant.noPolarityPrefab;
            polarity = MagnetPolarity.Attract; // doesn't matter, polarity disabled
        }
        if (roll < noPolarityChance)
        {
            prefab = variant.noPolarityPrefab;
            polarity = MagnetPolarity.Attract; // doesn't matter, polarity disabled
        }
        else if (roll < noPolarityChance + attractChance)
        {
            prefab = variant.attractPrefab;
            polarity = MagnetPolarity.Attract;
        }
        else
        {
            prefab = variant.repelPrefab;
            polarity = MagnetPolarity.Repel;
        }
        
        
        

        // Fallback: if a specific variant prefab isn't assigned, try others
        prefab ??= variant.attractPrefab ?? variant.repelPrefab ?? variant.noPolarityPrefab;
        if (prefab == null) return;

        GameObject prop = Instantiate(prefab, spawnPos.Value, Random.rotation);

        MetalObject metal = prop.GetComponent<MetalObject>();
        if (metal != null)
        {
            metal.objectType = MetalObject.ObjectType.Prop;
            metal.usePolarity = roll >= noPolarityChance;
            metal.polarity = polarity;
            metal.magneticStrength = Random.Range(300f, 800f);
            metal.range = Random.Range(rangeMin, rangeMin + 4f);
        }

        activeProps.Add(prop);
    }

    PropVariant PickPropVariant()
    {
        // Weighted random pick
        float totalWeight = 0f;
        foreach (var v in propVariants)
            totalWeight += v.spawnWeight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var v in propVariants)
        {
            cumulative += v.spawnWeight;
            if (roll <= cumulative)
                return v;
        }

        return propVariants[propVariants.Count - 1];
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    void CleanDestroyedObjects(List<GameObject> list)
    {
        list.RemoveAll(obj => obj == null);
    }

    Vector3? GetSpawnPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized
                                   * Random.Range(minSpawnDistance, spawnRadius);

            Vector3 candidate = new Vector3(
                player.position.x + randomCircle.x,
                player.position.y + groundRaycastHeight,
                player.position.z + randomCircle.y
            );

            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, groundRaycastHeight * 2f, groundLayer))
                return hit.point + Vector3.up * spawnHeight;
        }

        return null;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, spawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, minSpawnDistance);
    }
}