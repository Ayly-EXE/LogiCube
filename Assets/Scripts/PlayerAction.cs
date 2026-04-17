using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [Header("World")]
    public WorldManagerScript worldManager;
    public BlockType selectedBlock = BlockType.Stone;
    public CharacterController playerController;

    [Header("Weapon")]
    public Bow bow;

    void Start()
    {
        if (bow == null)
            bow = GetComponent<Bow>();

        if (bow == null)
            Debug.LogWarning("PlayerAction: ajoute le composant Bow sur le joueur.");
    }

    void Update()
    {
        bool bowEquipped = bow != null && bow.IsEquipped;

        if (!bowEquipped)
            HandleBlockActions();

        HandleTntIgnite();
    }

    void HandleBlockActions()
    {
        if (Input.GetMouseButtonDown(1))
            TryPlaceBlock();

        if (Input.GetMouseButtonDown(0))
            TryBreakBlock();
    }

    void HandleTntIgnite()
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        RaycastHit? hit = GetHit();
        if (!hit.HasValue)
            return;

        Vector3Int block = GetHitBlockPosition(hit.Value);

        if (worldManager.Blocks.TryGetValue(block, out BlockType blockType) && blockType == BlockType.Tnt)
            worldManager.Explode((Vector3)block);
    }

    RaycastHit? GetHit(float maxDistance = 100f)
    {
        Vector3 origin = transform.position + transform.forward * 0.1f;
        Ray ray = new Ray(origin, transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerObject(hit.transform))
                continue;

            return hit;
        }

        return null;
    }

    bool IsPlayerObject(Transform hitTransform)
    {
        if (playerController == null)
            return false;

        Transform playerRoot = playerController.transform;
        return hitTransform == playerRoot || hitTransform.IsChildOf(playerRoot);
    }

    public void ChangeBlockType(BlockType newBlockType)
    {
        selectedBlock = newBlockType;
        Debug.Log("Bloc choisi : " + selectedBlock);
    }

    Vector3Int GetHitBlockPosition(RaycastHit hit)
    {
        return Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
    }

    Vector3Int GetBlockPlacementPosition(RaycastHit hit)
    {
        Vector3Int hitBlockPos = GetHitBlockPosition(hit);
        return hitBlockPos + Vector3Int.RoundToInt(hit.normal);
    }

    bool IsInsidePlayer(Vector3Int blockPos)
    {
        if (playerController == null)
            return false;

        Bounds b = playerController.bounds;
        Vector3 min = b.min + Vector3.one * 0.001f;
        Vector3 max = b.max - Vector3.one * 0.001f;

        for (int x = Mathf.FloorToInt(min.x); x <= Mathf.FloorToInt(max.x); x++)
        {
            for (int y = Mathf.FloorToInt(min.y); y <= Mathf.FloorToInt(max.y); y++)
            {
                for (int z = Mathf.FloorToInt(min.z); z <= Mathf.FloorToInt(max.z); z++)
                {
                    if (new Vector3Int(x, y, z) == blockPos)
                        return true;
                }
            }
        }

        return false;
    }

    void TryPlaceBlock()
    {
        RaycastHit? hit = GetHit();
        if (!hit.HasValue)
            return;

        Vector3Int placePos = GetBlockPlacementPosition(hit.Value);

        if (!IsInsidePlayer(placePos))
            worldManager.PlaceBlock(placePos, selectedBlock);
    }

    void TryBreakBlock()
    {
        RaycastHit? hit = GetHit();
        if (!hit.HasValue)
            return;

        SlimeController slime = hit.Value.transform.GetComponentInParent<SlimeController>();
        if (slime != null)
        {
            slime.TakeHit();
            return;
        }

        Vector3Int hitBlockPos = GetHitBlockPosition(hit.Value);
        worldManager.DestroyBlock(hitBlockPos);
    }
}
