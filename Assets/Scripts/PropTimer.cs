using UnityEngine;

public class PropTimer : MonoBehaviour
{
    public float lifetime = 60f;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time >= spawnTime + lifetime)
            Destroy(gameObject);
    }
}