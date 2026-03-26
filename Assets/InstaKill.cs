using UnityEngine;

public class InstantKill : MonoBehaviour
{
    public string causeTag = "Spikes";

    private void OnCollisionEnter(Collision collision)
    {
        if (TryKillPlayer(collision.collider)) return;
        TryDestroyProp(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TryKillPlayer(other)) return;
        TryDestroyProp(other);
    }

    bool TryKillPlayer(Collider col)
    {
        PlayerHealth player = col.GetComponent<PlayerHealth>();
        if (player == null) return false;

        player.TakeDamage(99999f, causeTag);
        return true;
    }

    void TryDestroyProp(Collider col)
    {
        MetalObject metal = col.GetComponent<MetalObject>();
        if (metal != null && metal.objectType == MetalObject.ObjectType.Prop)
            Destroy(col.gameObject);
    }
}