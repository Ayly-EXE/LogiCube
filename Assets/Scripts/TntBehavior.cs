using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TntBehavior : MonoBehaviour
{

    public float radius = 5f;

    // Update is called once per frame
    Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        return Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
    }

    void Explode()
    {
        Vector3 origin = transform.position;

        List<RaycastHit> allHits = new List<RaycastHit>();

        float angleStep = 30f;

        for (float yaw = 0; yaw < 360; yaw += angleStep)
        {
            for (float pitch = -60; pitch <= 60; pitch += angleStep)
            {
                Vector3 direction =
                    Quaternion.Euler(pitch, yaw, 0) * Vector3.forward;

                RaycastHit[] hits =
                    Physics.RaycastAll(origin, direction, radius);

                foreach (var hit in hits)
                {
                    if (!allHits.Contains(hit))
                        allHits.Add(hit);
                }

                Debug.DrawRay(origin, direction * radius, Color.green, 1f);
            }
        }

        List<Vector3Int> allBlocks = new List<Vector3Int>();

        foreach (RaycastHit hit in allHits)
        {
            allBlocks.Add(GetHitBlockPosition(hit));
        }

        Debug.Log("Blocks hit: " + allBlocks.Count);
    }
}
