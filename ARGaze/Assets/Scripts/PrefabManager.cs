using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PrefabManager : MonoBehaviour
{
    public TextMeshProUGUI instructionText; // Assign in Inspector
    public GameObject[] prefabs; // Assign prefabs in Inspector
    public float spawnDistance = 2f; // Distance in front of camera
    public float spawnRadius = 1.5f; // 🔹 Radius for randomized placement

    private List<GameObject> spawnedObjects = new List<GameObject>();
    public GameObject targetObject;

    void Start()
    {
        SpawnAllPrefabs();
        SelectRandomTarget();
    }

    private void SpawnAllPrefabs()
    {
        if (prefabs.Length == 0) return;

        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 spawnCenter = cameraPosition + Camera.main.transform.forward * spawnDistance;

        for (int i = 0; i < prefabs.Length; i++)
        {
            float randomY = Random.Range(-spawnRadius, spawnRadius); // 🔹 Vertical variation
            float randomX = Random.Range(-spawnRadius, spawnRadius); // 🔹 Horizontal variation

            Vector3 spawnPosition = spawnCenter + new Vector3(randomX, randomY, 0);
            GameObject spawnedPrefab = Instantiate(prefabs[i], spawnPosition, Quaternion.identity);
            spawnedObjects.Add(spawnedPrefab);

            // Attach click handler dynamically
            var clickHandler = spawnedPrefab.AddComponent<PrefabClickHandler>();
            clickHandler.manager = this;
        }
    }

    public void SelectRandomTarget()
    {
        if (spawnedObjects.Count == 0)
        {
            instructionText.text = "All objects have been selected!";
            return;
        }

        int randomIndex = Random.Range(0, spawnedObjects.Count);
        targetObject = spawnedObjects[randomIndex];

        instructionText.text = $"Click on: {targetObject.name}";
    }

    public void RemoveObject(GameObject obj)
    {
        spawnedObjects.Remove(obj);
        Destroy(obj);
        SelectRandomTarget();
    }
}
