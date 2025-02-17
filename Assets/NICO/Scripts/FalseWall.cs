using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FalseWall : MonoBehaviour
{
    public int maxHealth = 3; // Vida máxima de cada tile
    private Tilemap tilemap;
    private Dictionary<Vector3Int, int> tileHealth = new Dictionary<Vector3Int, int>();

    private void Start()
    {
        tilemap = GetComponent<Tilemap>();

        // Inicializamos la vida de cada tile en la tilemap
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                tileHealth[pos] = maxHealth;
            }
        }
    }

    public void TakeDamage(Vector3 hitPosition)
    {
        Vector3Int cellPosition = tilemap.WorldToCell(hitPosition);

        if (tilemap.HasTile(cellPosition))
        {
            if (!tileHealth.ContainsKey(cellPosition))
            {
                tileHealth[cellPosition] = maxHealth; // Si el tile no estaba registrado, le damos vida inicial
            }

            tileHealth[cellPosition]--; // Reducimos la vida del tile

            Debug.Log($"Golpe en {cellPosition}, vida restante: {tileHealth[cellPosition]}");

            if (tileHealth[cellPosition] <= 0)
            {
                BreakWall(cellPosition);
            }
        }
        else
        {
            Debug.Log("⚠ No hay tile en esa posición.");
        }
    }

    private void BreakWall(Vector3Int cellPosition)
    {
        if (tilemap.HasTile(cellPosition))
        {
            tilemap.SetTile(cellPosition, null); // Elimina el tile
            tileHealth.Remove(cellPosition); // Eliminamos el tile del diccionario
            Debug.Log($"Tile destruido en: {cellPosition}");
        }
    }
}
