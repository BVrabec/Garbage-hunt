using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public GameObject[] trashPrefabs;
    public int amount = 20;

    [Header("Spawn Area")]
    public BoxCollider2D spawnArea;

    void Start()
    {
        for (int i = 0; i < amount; i++)
        {
            SpawnTrash();
        }
    }

    void SpawnTrash()
    {
        if (spawnArea == null)
        {
            Debug.LogError("NO SPAWN AREA ASSIGNED!");
            return;
        }

        Bounds b = spawnArea.bounds;

        Vector2 pos = new Vector2(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y)
        );

        GameObject prefab = trashPrefabs[
            Random.Range(0, trashPrefabs.Length)
        ];

        Instantiate(prefab, pos, Quaternion.identity);
    }
}