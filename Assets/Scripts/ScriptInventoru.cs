using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScriptInventory : MonoBehaviour
{
    public Sprite slotNormal;
    public Sprite slotSelected;

    private List<Image> slots = new();
    private int selectedSlot = 0;

    void Start()
    {
        // Automatically grab all slot images from children
        foreach (Transform child in transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
                slots.Add(img);
        }

        UpdateHotbarUI();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            int direction = scroll > 0 ? -1 : 1;
            selectedSlot = (selectedSlot + direction + slots.Count) % slots.Count;

            UpdateHotbarUI();
        }
    }

    void UpdateHotbarUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].sprite = (i == selectedSlot) ? slotSelected : slotNormal;
        }
    }
}