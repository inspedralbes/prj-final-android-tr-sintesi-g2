using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // El personaje u objeto que la cámara seguirá.

    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0, 5, -10); // Ajuste de la posición de la cámara respecto al objetivo.

    [Header("Smooth Movement")]
    public float smoothSpeed = 0.125f; // Controla la suavidad del movimiento de la cámara.

    private void LateUpdate()
    {
        if (target != null)
        {
            // Calcula la posición deseada.
            Vector3 desiredPosition = target.position + offset;

            // Interpola suavemente hacia la posición deseada.
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // Actualiza la posición de la cámara.
            transform.position = smoothedPosition;

            // Opcional: Orientar la cámara hacia el objetivo.
            transform.LookAt(target);
        }
        else
        {
            Debug.LogWarning("No se ha asignado un Target para la cámara.");
        }
    }
}
