using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public WorldManagerScript worldManager;

    void Start()
    {
        Destroy(gameObject, 7f);
    }

    void OnCollisionEnter(Collision collision)
    {
        SlimeController slime = collision.transform.GetComponentInParent<SlimeController>();
        if (slime != null)
            slime.TakeHit();

        if (worldManager != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            Vector3Int blockPos = Vector3Int.FloorToInt(contact.point - contact.normal * 0.01f);

            if (worldManager.Blocks.TryGetValue(blockPos, out BlockType blockType) && blockType == BlockType.Tnt)
                worldManager.Explode(blockPos);
        }

        Destroy(gameObject);
    }
}
