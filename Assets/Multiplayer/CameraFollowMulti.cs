using UnityEngine;
using Mirror;

public class CameraFollowMulti : MonoBehaviour
{
    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0, 5, -10); // Ajuste de la posición de la cámara respecto al objetivo.

    [Header("Smooth Movement")]
    public float smoothSpeed = 0.125f; // Suavidad del movimiento.

    private Transform target;

    private void Start()
    {
        FindLocalPlayer();
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Opcional: mira al target si quieres.
            // transform.LookAt(target);
        }
        else
        {
            // Si no tiene target, intenta buscarlo otra vez
            FindLocalPlayer();
        }
    }

    private void FindLocalPlayer()
    {
        foreach (var obj in FindObjectsOfType<NetworkIdentity>())
        {
            if (obj.isLocalPlayer)
            {
                target = obj.transform;
                Debug.Log($"Camera is now following: {target.name}");
                break;
            }
        }
    }
}
