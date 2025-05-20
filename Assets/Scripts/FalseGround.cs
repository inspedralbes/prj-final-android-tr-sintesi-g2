using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FalseGround : MonoBehaviour
{
    public Tilemap tilemap;
    private float timeToBreak = 5f; // Tiempo antes de que el suelo desaparezca
    private Dictionary<Vector3Int, Coroutine> breakingTiles = new Dictionary<Vector3Int, Coroutine>();

    private void Start()
    {
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Obtener todas las celdas bajo el jugador
            Vector3 worldPosition = other.transform.position;
            Vector3Int tilePos = tilemap.WorldToCell(worldPosition);

            if (tilemap.HasTile(tilePos) && !breakingTiles.ContainsKey(tilePos))
            {
                Coroutine coroutine = StartCoroutine(BreakTile(tilePos));
                breakingTiles[tilePos] = coroutine;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Asegurarse de que el tiempo se reinicie si el jugador abandona la celda
            Vector3 worldPosition = other.transform.position;
            Vector3Int tilePos = tilemap.WorldToCell(worldPosition);

            if (breakingTiles.ContainsKey(tilePos))
            {
                StopCoroutine(breakingTiles[tilePos]);
                breakingTiles.Remove(tilePos);
            }
        }
    }

    private IEnumerator BreakTile(Vector3Int tilePos)
    {
        yield return new WaitForSeconds(timeToBreak);

        if (tilemap.HasTile(tilePos))
        {
            tilemap.SetTile(tilePos, null); // Eliminar la celda del Tilemap
        }

        breakingTiles.Remove(tilePos);
    }
}