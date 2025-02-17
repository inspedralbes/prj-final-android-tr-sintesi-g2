using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FalseGround : MonoBehaviour
{
    public Tilemap tilemap;
    private float timeToBreak = 3f; // Ahora tarda 3 segundos en romperse
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
            Vector3Int tilePos = tilemap.WorldToCell(other.transform.position);
            if (!breakingTiles.ContainsKey(tilePos))
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
            Vector3Int tilePos = tilemap.WorldToCell(other.transform.position);
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
        tilemap.SetTile(tilePos, null);
        breakingTiles.Remove(tilePos);
    }
}
