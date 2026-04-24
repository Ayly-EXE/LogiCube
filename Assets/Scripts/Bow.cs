using UnityEngine;

public class Bow : MonoBehaviour
{
    public KeyCode equipKey = KeyCode.B;
    public GameObject bowPrefab;
    public WorldManagerScript worldManager;
    public CharacterController playerController;
    public Vector3 bowLocalPosition = new Vector3(0.32f, -0.32f, 0.62f);
    public float bowYRotation = -90f;
    public Vector3 bowLocalScale = Vector3.one * 0.35f;
    public Vector3 bowPullOffset = new Vector3(-0.10f, 0f, -0.07f);

    public bool IsEquipped => isEquipped;

    private Transform bowVisual;
    private bool isEquipped;
    private float pull;
    private Vector3 bowBaseLocalPosition;

    void Awake()
    {
        PlayerAction playerAction = GetComponent<PlayerAction>();
        if (playerAction != null)
        {
            if (worldManager == null)
                worldManager = playerAction.worldManager;
            if (playerController == null)
                playerController = playerAction.playerController;
        }
    }

    void Start()
    {
        bowVisual = Instantiate(bowPrefab, transform).transform;
        bowVisual.localPosition = bowLocalPosition;
        bowVisual.localEulerAngles = new Vector3(0f, bowYRotation, 0f);
        bowVisual.localScale = bowLocalScale;
        bowVisual.gameObject.SetActive(false);
        bowBaseLocalPosition = bowLocalPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(equipKey))
            ToggleBow();

        if (!isEquipped)
            return;

        UpdatePull();
        UpdateVisual();
    }

    void ToggleBow()
    {
        isEquipped = !isEquipped;
        pull = 0f;

        if (bowVisual != null)
            bowVisual.gameObject.SetActive(isEquipped);
    }

    void UpdatePull()
    {
        if (Input.GetMouseButton(0))
            pull = Mathf.Clamp01(pull + Time.deltaTime * 4f);

        if (Input.GetMouseButtonUp(0))
        {
            Shoot();
            pull = 0f;
        }
    }

    void UpdateVisual()
    {
        if (bowVisual == null)
            return;

        bowVisual.localPosition = bowBaseLocalPosition + bowPullOffset * pull;
    }

    void Shoot()
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = "Arrow";

        arrow.transform.position = transform.position + transform.forward * 0.6f + transform.right * 0.12f - transform.up * 0.08f;
        arrow.transform.rotation = Quaternion.FromToRotation(Vector3.up, transform.forward);
        arrow.transform.localScale = new Vector3(0.025f, 0.35f, 0.025f);

        Renderer renderer = arrow.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.30f, 0.18f, 0.08f);

        Collider arrowCollider = arrow.GetComponent<Collider>();
        if (arrowCollider != null && playerController != null)
        {
            Collider[] playerColliders = playerController.GetComponentsInChildren<Collider>();
            for (int i = 0; i < playerColliders.Length; i++)
                Physics.IgnoreCollision(arrowCollider, playerColliders[i], true);
        }

        Rigidbody rb = arrow.AddComponent<Rigidbody>();
        float speed = Mathf.Lerp(16f, 28f, pull);
        rb.velocity = transform.forward * speed;

        ArrowProjectile arrowScript = arrow.AddComponent<ArrowProjectile>();
        arrowScript.worldManager = worldManager;
    }
}
