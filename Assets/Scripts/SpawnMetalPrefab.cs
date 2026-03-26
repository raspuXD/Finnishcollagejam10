using UnityEngine;
using System.Collections;

public class SpawnMetalPrefab : MonoBehaviour
{
    public GameObject MetalObjectPrefab;

    [ContextMenu("SpawnObject")]
    public void SpawnObject()
    {
        Instantiate(MetalObjectPrefab);
    }
}
