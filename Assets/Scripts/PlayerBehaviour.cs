using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerBehaviour : MonoBehaviour
{
    [Min(0f)] public float speed = 5f;
    [SerializeField, Min(0f)] private float jumpSpeed = 6f;
    [SerializeField, Min(0.05f)] private float flipDuration = 0.5f;

    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0f)] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private bool isGrounded;

    private Rigidbody rb;
    private int jumpsRemaining = 2;
    private bool jumpRequested;
    private Quaternion facingRotation;
    private bool isFlipping;
    private float flipProgress;
    private int coinsCollected;
    private int totalCoins;
    private Text coinCounterText;
    private Canvas gameCanvas;
    private bool gameCompleted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        facingRotation = rb.rotation;
        rb.useGravity = true;
        // Las colisiones no deben volcar al personaje ni cambiar hacia donde mira.
        rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        HashSet<GameObject> coins = new HashSet<GameObject>();
        foreach (Collider coinCollider in FindObjectsByType<Collider>())
        {
            if (coinCollider.isTrigger && IsCoin(coinCollider.gameObject))
            {
                coins.Add(coinCollider.gameObject);
            }
        }

        totalCoins = coins.Count;
        CreateCoinCounter();
    }

    private static bool IsCoin(GameObject target)
    {
        return target.name.StartsWith("Coin", System.StringComparison.OrdinalIgnoreCase);
    }

    private void CreateCoinCounter()
    {
        GameObject canvasObject = new GameObject("Coin Counter Canvas", typeof(RectTransform), typeof(Canvas));
        gameCanvas = canvasObject.GetComponent<Canvas>();
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameCanvas.sortingOrder = 100;

        GameObject panelObject = new GameObject("Coin Counter Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(220f, 48f);
        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.65f);
        panel.raycastTarget = false;

        GameObject textObject = new GameObject("Coin Counter Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        coinCounterText = textObject.GetComponent<Text>();
        coinCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinCounterText.fontSize = 24;
        coinCounterText.alignment = TextAnchor.MiddleCenter;
        coinCounterText.color = Color.white;
        coinCounterText.raycastTarget = false;
        UpdateCoinCounter();
    }

    private void UpdateCoinCounter()
    {
        coinCounterText.text = $"Monedas: {coinsCollected}/{totalCoins}";
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (gameCompleted)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (gameCompleted)
        {
            return;
        }

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundCheckRadius,
                groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        if (jumpRequested)
        {
            jumpRequested = false;

            if (jumpsRemaining > 0)
            {
                bool isDoubleJump = jumpsRemaining == 1;
                jumpsRemaining--;
                Vector3 velocity = rb.linearVelocity;
                velocity.y = jumpSpeed;
                rb.linearVelocity = velocity;

                if (isDoubleJump)
                {
                    isFlipping = true;
                    flipProgress = 0f;
                }
            }
        }

        ApplyMovement();

        if (isFlipping)
        {
            flipProgress = Mathf.Min(1f, flipProgress + Time.fixedDeltaTime / flipDuration);
            ApplyFlipRotation();

            if (flipProgress >= 1f)
            {
                isFlipping = false;
            }
        }
    }

    private void ApplyMovement()
    {
        // La boquita esta en +X local, no en +Z (forward).
        Vector3 direction = Vector3.ProjectOnPlane(facingRotation * Vector3.right, Vector3.up).normalized;
        Vector3 velocity = direction * speed;
        // Conserva la caida que produce la gravedad.
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void ApplyFlipRotation()
    {
        rb.rotation = facingRotation * Quaternion.AngleAxis(-360f * flipProgress, Vector3.forward);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameCompleted)
        {
            return;
        }

        Vector3 direction = Vector3.ProjectOnPlane(facingRotation * Vector3.right, Vector3.up).normalized;
        bool hitWall = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            if (normal.y > 0.5f)
            {
                jumpsRemaining = 2;
            }

            // Solo gira ante una pared de frente; el piso no cuenta.
            if (Mathf.Abs(normal.y) < 0.5f && Vector3.Dot(direction, normal) < -0.5f)
            {
                hitWall = true;
            }
        }

        if (hitWall)
        {
            facingRotation = Quaternion.AngleAxis(-180f, Vector3.up) * facingRotation;
            ApplyFlipRotation();
            ApplyMovement();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameCompleted || !IsCoin(other.gameObject) || !other.gameObject.activeSelf)
        {
            return;
        }

        other.gameObject.SetActive(false);
        coinsCollected++;
        UpdateCoinCounter();
        Destroy(other.gameObject);

        if (totalCoins > 0 && coinsCollected >= totalCoins)
        {
            CompleteGame();
        }
    }

    private void CompleteGame()
    {
        gameCompleted = true;
        jumpRequested = false;
        isFlipping = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        GameObject panelObject = new GameObject("Victory Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(gameCanvas.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(480f, 180f);
        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.85f);
        panel.raycastTarget = false;

        GameObject textObject = new GameObject("Victory Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);
        Text victoryText = textObject.GetComponent<Text>();
        victoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        victoryText.fontSize = 28;
        victoryText.alignment = TextAnchor.MiddleCenter;
        victoryText.color = Color.white;
        victoryText.raycastTarget = false;
        victoryText.text = "Ganaste!\nRecolectaste todas las monedas\nPresiona R para reiniciar";
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
