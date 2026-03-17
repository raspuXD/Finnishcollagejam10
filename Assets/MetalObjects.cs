using UnityEngine;

public class MetalObject : MonoBehaviour
{
    public float magneticStrength = 1000f;

    [HideInInspector] public Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
}