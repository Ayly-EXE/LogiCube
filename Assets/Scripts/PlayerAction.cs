using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    public GameObject blockPrefab;
    public WorldManagerScript worldManager; 
    // Raycast to get the first solid object hit
    public RaycastHit? GetHit(float maxDistance = 100f)
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            return hit; // Return the RaycastHit
        }

        return null; // Nothing hit
    }


    // Get the voxel position for block placement (only adjacent face)
    public Vector3 GetBlockPlacementPosition(RaycastHit hit)
    {
        // Get the hit block position (floor to voxel)
        Vector3 hitBlockPos = new Vector3(
            Mathf.FloorToInt(hit.transform.position.x),
            Mathf.FloorToInt(hit.transform.position.y),
            Mathf.FloorToInt(hit.transform.position.z)
        );

        // Determine which face was hit
        Vector3 normal = hit.normal;

        // Place the new block adjacent along the axis of the normal
        Vector3 placePos = hitBlockPos;

        if (Mathf.Abs(normal.x) > 0.5f) placePos.x += normal.x > 0 ? 1 : -1;
        else if (Mathf.Abs(normal.y) > 0.5f) placePos.y += normal.y > 0 ? 1 : -1;
        else if (Mathf.Abs(normal.z) > 0.5f) placePos.z += normal.z > 0 ? 1 : -1;

        return placePos;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit? hit = GetHit();
            if (hit.HasValue)
            {
                Vector3 blockPos = GetBlockPlacementPosition(hit.Value);
                //Instantiate(blockPrefab, blockPos, Quaternion.identity);
                worldManager.PlayerPlacedBlock(blockPos, blockPrefab.name);
                Debug.Log("Block placed at: " + blockPos);
            }
            else
            {
                Debug.Log("Nothing hit");
            }
        }
    
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit? hit = GetHit();
            if (hit.HasValue)
            {
                GameObject hitObject = hit.Value.collider.gameObject;

                if (hitObject.CompareTag("Block"))
                {
                    //Destroy(hitObject);
                    worldManager.PlayerDestroyedBlock(hitObject);
                    Debug.Log("Block destroyed: " + hitObject.name);
                }
                else
                {
                    Debug.Log("Hit object is not a block: " + hitObject.name);
                }
            }

            else
            {
                Debug.Log("Nothing hit");
            }
        }
    }
    
}
