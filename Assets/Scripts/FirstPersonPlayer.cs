using UnityEngine;

// Ce script a besoin d'un CharacterController sur le même objet
[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayer : MonoBehaviour
{
    [Header("Déplacements")]
    public float moveSpeed = 5f;          // vitesse pour marcher
    public float flySpeed = 7f;           // vitesse pour voler
    public float lookSpeed = 2f;          // vitesse de la souris
    public float gravity = -20f;          // force qui fait tomber
    public float jumpHeight = 1.2f;       // hauteur du saut
    public float doubleTapDelay = 0.3f;   // temps max entre 2 appuis sur espace

    [Header("Références")]
    public Transform cameraTransform;     // la caméra du joueur

    private CharacterController controller;

    private float pitch = 0f;             // rotation verticale de la caméra
    private float verticalVelocity = 0f;  // vitesse vers le haut ou vers le bas

    private bool isFlying = false;        // est-ce que le joueur vole ?
    private float lastSpacePressTime = -10f; // moment du dernier appui sur espace

    void Start()
    {
        // On récupère le CharacterController
        controller = GetComponent<CharacterController>();

        // On cache et bloque la souris au centre de l'écran
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // À chaque image, on regarde, on gère le saut/vol, puis on bouge
        Look();
        HandleJumpAndFlyToggle();
        Move();

        // Touche Échap : libère la souris
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Look()
    {
        // On lit le mouvement de la souris
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        // Tourner à gauche/droite = tourner le joueur entier
        transform.Rotate(0f, mouseX, 0f, Space.Self);

        // On enlève tout penchement bizarre du corps
        Vector3 bodyEuler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, bodyEuler.y, 0f);

        // Regarder en haut/bas = tourner seulement la caméra
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f); // limite pour ne pas tourner complètement
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleJumpAndFlyToggle()
    {
        // Si on n'appuie pas sur espace, on ne fait rien
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        float now = Time.time;

        // Vérifie si on a appuyé 2 fois rapidement sur espace
        bool isDoubleTap = now - lastSpacePressTime <= doubleTapDelay;
        lastSpacePressTime = now;

        // Si on est dans les airs et qu'on fait un double appui :
        // on active ou désactive le mode vol
        if (!controller.isGrounded && isDoubleTap)
        {
            isFlying = !isFlying;

            // Si on commence à voler, on arrête la chute
            if (isFlying)
            {
                verticalVelocity = 0f;
            }

            return;
        }

        // Saut normal : seulement si on est au sol et qu'on ne vole pas
        if (controller.isGrounded && !isFlying)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Move()
    {
        // Lire les touches du clavier
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Crée la direction de déplacement
        Vector3 horizontalMove = (transform.right * x + transform.forward * z).normalized;

        // Si on est en mode vol
        if (isFlying)
        {
            float verticalFly = 0f;

            // Espace = monter
            if (Input.GetKey(KeyCode.Space))
                verticalFly += 1f;

            // Shift = descendre
            if (Input.GetKey(KeyCode.LeftShift))
                verticalFly -= 1f;

            Vector3 move = horizontalMove * flySpeed + Vector3.up * verticalFly * flySpeed;
            controller.Move(move * Time.deltaTime);
            return;
        }

        // Si on touche le sol et qu'on tombe encore un peu, on remet une petite valeur
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // La gravité fait descendre le joueur
        verticalVelocity += gravity * Time.deltaTime;

        // Déplacement final
        Vector3 velocity = horizontalMove * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}