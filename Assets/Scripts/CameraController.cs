using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;

    private Vector3 previousPlayerPosition;

    // Conecta el seguimiento al iniciar y cada vez que se recarga la escena.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterCameraTracking()
    {
        SceneManager.sceneLoaded -= AttachToMainCamera;
        SceneManager.sceneLoaded += AttachToMainCamera;
        AttachToMainCamera(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void AttachToMainCamera(Scene scene, LoadSceneMode mode)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CameraController>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraController>();
        }
    }

    private void Start()
    {
        if (player == null)
        {
            PlayerBehaviour playerBehaviour = FindAnyObjectByType<PlayerBehaviour>();
            if (playerBehaviour != null)
            {
                player = playerBehaviour.transform;
            }
        }

        if (player == null)
        {
            Debug.LogError("No se encontro al Player para que la camara lo siga.", this);
            enabled = false;
            return;
        }

        previousPlayerPosition = player.position;
    }

    private void LateUpdate()
    {
        if (player != null)
        {
            // Acompana el desplazamiento del Player desde la posicion actual de la camara.
            transform.position += player.position - previousPlayerPosition;
            previousPlayerPosition = player.position;
        }
    }
}
