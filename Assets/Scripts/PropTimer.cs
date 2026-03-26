using UnityEngine;

public class PropTimer : MonoBehaviour
{
    public float lifetime = 60f;
    private float spawnTime;
    [SerializeField]private string SoundEffect;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time >= spawnTime + lifetime)
            Destroy(gameObject);
    }

    public void OnCollisionEnter(Collision collision)
    {
        AudioManager.Instance.PlaySFX3D(SoundEffect, this.gameObject, 1f, 35f);
    }
}