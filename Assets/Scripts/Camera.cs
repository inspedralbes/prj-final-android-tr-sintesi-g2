using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0, 5, -10);

    [Header("Smooth Movement")]
    public float smoothSpeed = 0.125f;

    private bool hasSaved = false;
    private Checkpoint checkpoint;

    void Start()
    {
        checkpoint = FindObjectOfType<Checkpoint>(); // Encuentra el checkpoint en la escena
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            if (!hasSaved && checkpoint != null)
            {
                checkpoint.SaveGame();
                hasSaved = true;
            }

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
            transform.LookAt(target);
        }
        else
        {
            Debug.LogWarning("No se ha asignado un Target para la cámara.");
        }
    }
}
