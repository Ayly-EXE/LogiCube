using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class editor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GameObject referenceBlock;
    [SerializeField] private Transform placedBlocksRoot;
    [SerializeField] private TMP_InputField saveInputField;

    [Header("Blocks")]
    [SerializeField] private List<BlockPrefabEntry> blockPrefabs = new();
    [SerializeField] private BlockType selectedBlock = BlockType.Stone;
    [SerializeField] private bool logBlockSelection = true;

    [Header("Fly Controls")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float flySpeed = 7f;
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private float sprintMultiplier = 2.5f;

    [Header("Build")]
    [SerializeField] private float maxBuildDistance = 12f;
    [SerializeField] private LayerMask buildMask = ~0;
    [SerializeField] private bool preventBuildWhenPointerOverUI = false;

    [Header("Save")]
    [SerializeField] private string saveFolderName = "SavedStructures";
    [SerializeField] private string saveFilePrefix = "structure";

    private readonly Dictionary<Vector3Int, PlacedBlockData> placedBlocks = new();
    private readonly Dictionary<BlockType, GameObject> prefabByType = new();

    private Vector3 gridOrigin = Vector3.zero;
    private Vector3 cellSize = Vector3.one;

    private float yaw;
    private float pitch;

    private string fileName;

    private void Awake()
    {
        AutoAssignReferences();
        AutoFillPrefabsFromProject();
        CacheGridValues();
        BuildPrefabDictionary();
        EnsureSelectedBlockIsValid();
        SyncPlacedBlocksFromScene();
    }

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = GetNormalizedPitch(cameraTransform != null ? cameraTransform.localEulerAngles.x : 0f);
        SetCursorLocked(true);
        LogSelectedBlock();

        if (saveInputField != null)
        {
            saveInputField.onSelect.AddListener(_ => SetCursorLocked(false));
            saveInputField.onDeselect.AddListener(_ => SetCursorLocked(true));
        }
    }

    private void Update()
    {
        HandleCursorToggle();
        HandleInputFieldFocus();

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        HandleLook();
        HandleFlyMovement();
        HandleBuildInput();
    }

    public void ChangeBlockType(BlockType newBlockType)
    {
        if (!prefabByType.ContainsKey(newBlockType))
        {
            Debug.LogWarning($"[EditorScene] Aucun prefab configure pour {newBlockType}.");
            return;
        }

        selectedBlock = newBlockType;
        LogSelectedBlock();
    }

    public BlockType GetSelectedBlockType()
    {
        return selectedBlock;
    }

    private void AutoAssignReferences()
    {
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                cameraTransform = mainCamera.transform;
            else
                cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }

        if (referenceBlock == null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
                referenceBlock = ground;
        }

        if (placedBlocksRoot == null)
        {
            GameObject existingRoot = GameObject.Find("PlacedBlocks");
            if (existingRoot != null)
                placedBlocksRoot = existingRoot.transform;
            else
                placedBlocksRoot = new GameObject("PlacedBlocks").transform;
        }
    }

    private void CacheGridValues()
    {
        if (referenceBlock == null)
        {
            gridOrigin = Vector3.zero;
            cellSize = Vector3.one;
            return;
        }

        gridOrigin = referenceBlock.transform.position;
        Vector3 refScale = referenceBlock.transform.lossyScale;

        cellSize = new Vector3(
            Mathf.Abs(refScale.x) > 0.0001f ? Mathf.Abs(refScale.x) : 1f,
            Mathf.Abs(refScale.y) > 0.0001f ? Mathf.Abs(refScale.y) : 1f,
            Mathf.Abs(refScale.z) > 0.0001f ? Mathf.Abs(refScale.z) : 1f
        );
    }

    private void BuildPrefabDictionary()
    {
        prefabByType.Clear();
        blockPrefabs.RemoveAll(static entry => entry == null || entry.prefab == null);

        for (int i = 0; i < blockPrefabs.Count; i++)
        {
            BlockPrefabEntry entry = blockPrefabs[i];
            if (!prefabByType.ContainsKey(entry.type))
                prefabByType.Add(entry.type, entry.prefab);
            else
                prefabByType[entry.type] = entry.prefab;
        }

        if (prefabByType.Count == 0 && referenceBlock != null)
        {
            prefabByType[selectedBlock] = referenceBlock;
            blockPrefabs.Add(new BlockPrefabEntry { type = selectedBlock, prefab = referenceBlock });
        }
    }

    private void EnsureSelectedBlockIsValid()
    {
        if (prefabByType.ContainsKey(selectedBlock))
            return;

        foreach (KeyValuePair<BlockType, GameObject> pair in prefabByType)
        {
            selectedBlock = pair.Key;
            return;
        }
    }

    private void LogSelectedBlock()
    {
        if (logBlockSelection)
            Debug.Log($"[EditorScene] Bloc selectionne: {selectedBlock}");
    }

    private void SyncPlacedBlocksFromScene()
    {
        placedBlocks.Clear();

        if (placedBlocksRoot == null)
            return;

        for (int i = 0; i < placedBlocksRoot.childCount; i++)
        {
            Transform child = placedBlocksRoot.GetChild(i);
            Vector3Int gridPos = WorldToGrid(child.position);

            if (!placedBlocks.ContainsKey(gridPos))
            {
                placedBlocks.Add(gridPos, new PlacedBlockData
                {
                    instance = child.gameObject,
                    type = ExtractBlockTypeFromName(child.name)
                });
            }
        }
    }

    private void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool lockCursor = Cursor.lockState != CursorLockMode.Locked;
            SetCursorLocked(lockCursor);
        }

        if (Input.GetMouseButtonDown(0) && IsPointerOverUI())
        {
            SetCursorLocked(false);
        }
    }

    private void HandleInputFieldFocus()
    {
        if (!Input.GetKeyDown(KeyCode.I))
            return;

        if (saveInputField == null)
            return;

        if (saveInputField.isFocused)
        {
            saveInputField.DeactivateInputField();
            SetCursorLocked(true);
        }
        else
        {
            saveInputField.ActivateInputField();
            SetCursorLocked(false);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleFlyMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (Input.GetKey(KeyCode.Space))
            move += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            move += Vector3.down;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        float currentSpeed = flySpeed;
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            currentSpeed *= sprintMultiplier;

        transform.position += move * currentSpeed * Time.deltaTime;
    }

    private void HandleBuildInput()
    {
        if (saveInputField != null && saveInputField.isFocused)
            return;

        if (Input.GetMouseButtonDown(1))
            TryPlaceBlock();

        if (Input.GetMouseButtonDown(0))
            TryBreakBlock();
    }

    public void ReadStringInput(string s)
    {
        fileName = s;
        SaveToJson();
    }

    private void TryPlaceBlock()
    {
        if (preventBuildWhenPointerOverUI && IsPointerOverUI())
            return;

        if (!TryGetPrefabForType(selectedBlock, out GameObject prefab))
        {
            Debug.LogWarning($"[EditorScene] Aucun prefab configure pour {selectedBlock}.");
            return;
        }

        if (!TryGetBuildHit(out RaycastHit hit))
            return;

        Vector3Int hitGrid = WorldToGrid(hit.point - hit.normal * 0.01f);
        Vector3Int placeGrid = hitGrid + NormalToGridOffset(hit.normal);

        if (placedBlocks.ContainsKey(placeGrid))
            return;

        GameObject newBlock = Instantiate(prefab, GridToWorld(placeGrid), prefab.transform.rotation, placedBlocksRoot);
        newBlock.name = $"Block::{(int)selectedBlock}::{placeGrid.x}_{placeGrid.y}_{placeGrid.z}";

        placedBlocks[placeGrid] = new PlacedBlockData
        {
            instance = newBlock,
            type = selectedBlock
        };
    }

    private void TryBreakBlock()
    {
        if (preventBuildWhenPointerOverUI && IsPointerOverUI())
            return;

        if (!TryGetBuildHit(out RaycastHit hit))
            return;

        Transform blockRoot = GetBlockRootUnderPlacedRoot(hit.transform);
        if (blockRoot == null)
            return;

        Vector3Int gridPos = WorldToGrid(blockRoot.position);
        placedBlocks.Remove(gridPos);

        Destroy(blockRoot.gameObject);
    }

    private bool TryGetBuildHit(out RaycastHit hit)
    {
        hit = default;

        if (cameraTransform == null)
            return false;

        Ray ray = new(cameraTransform.position, cameraTransform.forward);
        return Physics.Raycast(ray, out hit, maxBuildDistance, buildMask, QueryTriggerInteraction.Ignore);
    }

    private Transform GetBlockRootUnderPlacedRoot(Transform source)
    {
        if (source == null || placedBlocksRoot == null)
            return null;

        Transform current = source;
        while (current != null && current.parent != placedBlocksRoot)
        {
            if (current == placedBlocksRoot)
                return null;
            current = current.parent;
        }

        return current;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void AutoFillPrefabsFromProject()
    {
#if UNITY_EDITOR
        TryAddPrefabIfMissing(BlockType.Dirt, "Assets/Prefabs/DirtCube.prefab");
        TryAddPrefabIfMissing(BlockType.Stone, "Assets/Prefabs/StoneCube.prefab");
        TryAddPrefabIfMissing(BlockType.Grass, "Assets/Prefabs/GrassCube.prefab");
        TryAddPrefabIfMissing(BlockType.Tnt, "Assets/Prefabs/TntPrimed.prefab");
#endif
    }

#if UNITY_EDITOR
    private void TryAddPrefabIfMissing(BlockType type, string assetPath)
    {
        for (int i = 0; i < blockPrefabs.Count; i++)
        {
            if (blockPrefabs[i] != null && blockPrefabs[i].type == type && blockPrefabs[i].prefab != null)
                return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
            return;

        blockPrefabs.Add(new BlockPrefabEntry
        {
            type = type,
            prefab = prefab
        });
    }
#endif

    private bool TryGetPrefabForType(BlockType type, out GameObject prefab)
    {
        return prefabByType.TryGetValue(type, out prefab) && prefab != null;
    }

    private Vector3Int WorldToGrid(Vector3 world)
    {
        Vector3 local = world - gridOrigin;
        return new Vector3Int(
            Mathf.RoundToInt(local.x / cellSize.x),
            Mathf.RoundToInt(local.y / cellSize.y),
            Mathf.RoundToInt(local.z / cellSize.z)
        );
    }

    private Vector3 GridToWorld(Vector3Int grid)
    {
        return gridOrigin + Vector3.Scale((Vector3)grid, cellSize);
    }

    private static Vector3Int NormalToGridOffset(Vector3 normal)
    {
        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y) && Mathf.Abs(normal.x) > Mathf.Abs(normal.z))
            return new Vector3Int(Mathf.RoundToInt(Mathf.Sign(normal.x)), 0, 0);
        if (Mathf.Abs(normal.y) > Mathf.Abs(normal.x) && Mathf.Abs(normal.y) > Mathf.Abs(normal.z))
            return new Vector3Int(0, Mathf.RoundToInt(Mathf.Sign(normal.y)), 0);

        return new Vector3Int(0, 0, Mathf.RoundToInt(Mathf.Sign(normal.z)));
    }

    private void SaveToJson()
    {
        SyncPlacedBlocksFromScene();

        List<BlockEntry> entries = new();
        foreach (KeyValuePair<Vector3Int, PlacedBlockData> pair in placedBlocks)
        {
            if (pair.Value.instance == null)
                continue;

            entries.Add(new BlockEntry
            {
                type = pair.Value.type,
                x = pair.Key.x,
                y = pair.Key.y,
                z = pair.Key.z
            });
        }

        entries.Sort(static (a, b) =>
        {
            int cmpX = a.x.CompareTo(b.x);
            if (cmpX != 0) return cmpX;

            int cmpY = a.y.CompareTo(b.y);
            if (cmpY != 0) return cmpY;

            int cmpZ = a.z.CompareTo(b.z);
            if (cmpZ != 0) return cmpZ;

            return a.type.CompareTo(b.type);
        });

        StructureData data = new()
        {
            structureName = saveFilePrefix,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            origin = new Float3(gridOrigin),
            cellSize = new Float3(cellSize),
            blockCount = entries.Count,
            blocks = entries.ToArray()
        };

        string json = JsonUtility.ToJson(data, true);
        string folderPath = Path.Combine(Application.dataPath, saveFolderName);
        Directory.CreateDirectory(folderPath);

        string file = $"{fileName}.json";
        string fullPath = Path.Combine(folderPath, file);
        File.WriteAllText(fullPath, json);

        Debug.Log($"[EditorScene] Structure enregistree: {fullPath}");
    }

    private BlockType ExtractBlockTypeFromName(string objectName)
    {
        const string marker = "Block::";
        int markerIndex = objectName.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
            return selectedBlock;

        int typeStart = markerIndex + marker.Length;
        int typeEnd = objectName.IndexOf("::", typeStart, StringComparison.Ordinal);
        if (typeEnd < 0)
            return selectedBlock;

        string rawType = objectName.Substring(typeStart, typeEnd - typeStart);
        if (int.TryParse(rawType, out int typeValue) && Enum.IsDefined(typeof(BlockType), typeValue))
            return (BlockType)typeValue;

        return selectedBlock;
    }

    private static float GetNormalizedPitch(float eulerX)
    {
        if (eulerX > 180f)
            eulerX -= 360f;

        return eulerX;
    }

    [Serializable]
    private class BlockPrefabEntry
    {
        public BlockType type = BlockType.Stone;
        public GameObject prefab;
    }

    [Serializable]
    private struct StructureData
    {
        public string structureName;
        public string savedAtUtc;
        public Float3 origin;
        public Float3 cellSize;
        public int blockCount;
        public BlockEntry[] blocks;
    }

    [Serializable]
    private struct BlockEntry
    {
        public BlockType type;
        public int x;
        public int y;
        public int z;
    }

    private struct PlacedBlockData
    {
        public GameObject instance;
        public BlockType type;
    }

    [Serializable]
    private struct Float3
    {
        public float x;
        public float y;
        public float z;

        public Float3(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }
    }
}