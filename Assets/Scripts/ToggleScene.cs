using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToggleScene : MonoBehaviour
{
    private const string SampleScene = "SampleScene";
    private const string EditorScene = "EditorScene";

    private static ToggleScene instance;
    private static bool hasCachedPose;
    private static bool shouldRestorePoseOnSampleLoad;
    private static Vector3 cachedPosition;
    private static Quaternion cachedRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null)
            return;

        GameObject go = new("ToggleScene");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ToggleScene>();
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.C))
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == SampleScene)
        {
            SaveAndCacheSamplePose();
            SceneManager.LoadScene(EditorScene);
            return;
        }

        if (sceneName == EditorScene)
            SceneManager.LoadScene(SampleScene);
    }

    private static void SaveAndCacheSamplePose()
    {
        WorldManagerScript worldManager = FindFirstObjectByType<WorldManagerScript>();
        if (worldManager == null || worldManager.player == null)
            return;

        Transform player = worldManager.player.transform;
        cachedPosition = player.position;
        cachedRotation = player.rotation;
        hasCachedPose = true;
        shouldRestorePoseOnSampleLoad = true;

        worldManager.SaveWorld();
        PlayerPrefs.Save();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SampleScene || !hasCachedPose || !shouldRestorePoseOnSampleLoad)
            return;

        StartCoroutine(RestorePoseNextFrame());
    }

    private IEnumerator RestorePoseNextFrame()
    {
        yield return null;

        WorldManagerScript worldManager = FindFirstObjectByType<WorldManagerScript>();
        if (worldManager == null || worldManager.player == null)
            yield break;

        Transform player = worldManager.player.transform;
        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null) controller.enabled = false;
        player.SetPositionAndRotation(cachedPosition, cachedRotation);
        if (controller != null) controller.enabled = true;

        worldManager.SaveWorld();
        shouldRestorePoseOnSampleLoad = false;
    }
}
