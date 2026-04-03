using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WorldSelectionMenuUI : MonoBehaviour
{
    [Header("References")]
    public WorldManagerScript worldManager;
    public Camera menuCamera;
    public GameObject panel;
    public TMP_Dropdown worldsDropdown;
    public TMP_InputField newWorldInput;
    public TMP_Text statusText;

    private const string NoWorldLabel = "(aucun monde)";

    void Start()
    {
        if (worldManager == null)
            worldManager = FindFirstObjectByType<WorldManagerScript>();

        if (worldManager != null && worldManager.IsWorldInitialized)
        {
            if (menuCamera != null)
                menuCamera.enabled = false;

            if (panel != null)
                panel.SetActive(false);
            return;
        }

        if (menuCamera != null)
            menuCamera.enabled = true;

        if (panel != null)
            panel.SetActive(true);

        RefreshWorldList();
        SetStatus("");
        ForceMenuCursor();
    }

    void Update()
    {
        if (!worldManager.IsWorldInitialized)
        {
            if (menuCamera != null)
                menuCamera.enabled = true;

            if (panel != null && !panel.activeSelf)
                panel.SetActive(true);

            ForceMenuCursor();
        }
    }

    public void RefreshWorldList()
    {
        List<string> ids = WorldManagerScript.GetAvailableWorldIds();
        worldsDropdown.ClearOptions();

        if (ids.Count == 0)
            ids.Add(NoWorldLabel);

        worldsDropdown.AddOptions(ids);
    }

    public void LoadSelectedWorld()
    {
        //aucun monde crée donc 0 élément dans le dropdown
        if (worldsDropdown == null || worldsDropdown.options.Count == 0)
        {
            SetStatus("Pas de monde a charger.");
            return;
        }
        string id = worldsDropdown.options[worldsDropdown.value].text;
        if (id == NoWorldLabel)
        {
            SetStatus("Pas de monde a charger.");
            return;
        }

        StartWorld(id);
    }

    public void CreateAndStartWorld()
    {
        string id = newWorldInput != null ? newWorldInput.text : "";
        id = WorldManagerScript.NormalizeWorldId(id);

        if (!WorldManagerScript.IsValidWorldId(id))
        {
            SetStatus("Nom invalide. Utilise lettres/chiffres/_/-");
            return;
        }

        List<string> worlds = WorldManagerScript.GetAvailableWorldIds();
        if (worlds.Contains(id))
        {
            SetStatus("Ce monde existe deja.");
            return;
        }

        StartWorld(id);
    }

    private void StartWorld(string worldId)
    {

        WorldManagerScript.SelectWorld(worldId);
        worldManager.StartSelectedWorld();

        if (panel != null)
            panel.SetActive(false);

        if (menuCamera != null)
            menuCamera.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetStatus("");
    }

    private void ForceMenuCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (!string.IsNullOrEmpty(message))
            Debug.Log(message);
    }
}
