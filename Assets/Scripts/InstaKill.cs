using UnityEngine;

public class InstantKill : MonoBehaviour
{
    [SerializeField] private string causeTag = "Spikes";
    private const float instantKillDamage = 99999f;

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider col)
    {
        if (TryKillPlayer(col)) return;
        TryDestroyProp(col);
    }

    private bool TryKillPlayer(Collider col)
    {
        if (!col.TryGetComponent<PlayerHealth>(out var player))
            return false;

        player.TakeDamage(instantKillDamage, causeTag);
        return true;
    }

    private void TryDestroyProp(Collider col)
    {
        if (!col.TryGetComponent<MetalObject>(out var metal))
            return;

        if (metal.objectType == MetalObject.ObjectType.Prop)
            Destroy(col.gameObject);
    }
}