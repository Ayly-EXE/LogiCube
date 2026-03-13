using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScriptInventory : MonoBehaviour
{
    public Sprite slotNormal;
    public Sprite slotSelected;

    public PlayerAction playerAction;

    private List<Image> slots = new();
    private int selectedSlot = 0;

    private List<BlockType> hotbar_slots = new()
    {
        BlockType.Dirt,
        BlockType.Grass,
        BlockType.Stone,
    };

    [System.Serializable]
    public class BlockSpriteEntry
    {
        public BlockType type;
        public Sprite sprite;
    }


    public List<BlockSpriteEntry> blockSpritesList = new();
    private Dictionary<BlockType, Sprite> blockSprites;

    

    void Awake()
    {

        playerAction = FindObjectOfType<PlayerAction>();

        foreach (Transform child in transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
                slots.Add(img);
        }


        // Crée un dictionnaire pour accéder rapidement aux sprites par type de bloc

        blockSprites = new Dictionary<BlockType, Sprite>();

        foreach (var entry in blockSpritesList)
        {
            blockSprites[entry.type] = entry.sprite;
        }

        for (int i = 0; i < hotbar_slots.Count; i++)
        {
            BlockType type = hotbar_slots[i];

            // Si on a un sprite pour ce type de bloc, on l'affiche dans le slot correspondant
            if (blockSprites.TryGetValue(type, out Sprite sprite))
            {
                // Crée une Image enfant pour afficher le sprite du bloc
                GameObject blockImgGO = new GameObject("BlockImage");
                blockImgGO.transform.SetParent(slots[i].transform);
                blockImgGO.transform.localPosition = Vector3.zero;
                blockImgGO.transform.localScale = new Vector3(0.8f, 0.8f, 1f); // Ajuste la taille du sprite dans le slot

                Image blockImg = blockImgGO.AddComponent<Image>();
                blockImg.sprite = sprite;
            }
    }



        playerAction.ChangeBlockType(hotbar_slots[0]);

        UpdateHotbarSelectedUI();
    }



    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            int direction = scroll > 0 ? -1 : 1;
            selectedSlot = (selectedSlot + direction + slots.Count) % slots.Count;

            UpdateHotbarSelectedUI();
            updatePlayerAction();
        }
    }

    void UpdateHotbarSelectedUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].sprite = (i == selectedSlot) ? slotSelected : slotNormal;
        }
    }

    void updatePlayerAction()
    {
        if (selectedSlot < hotbar_slots.Count)
            playerAction.ChangeBlockType(hotbar_slots[selectedSlot]);
    }

    
}