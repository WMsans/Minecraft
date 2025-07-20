using UnityEngine;

public class TerrainEditor : MonoBehaviour
{
    public TerrainManager terrainManager;
    public Camera playerCamera;
    public float modificationStrength = 5f;
    public float modificationRadius = 2f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click to dig
        {
            ModifyTerrain(true);
        }
        else if (Input.GetMouseButtonDown(1)) // Right-click to place
        {
            ModifyTerrain(false);
        }
    }

    private void ModifyTerrain(bool dig)
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            float strength = dig ? -modificationStrength : modificationStrength;
            terrainManager.ModifyTerrain(hit.point, strength, modificationRadius);
        }
    }
}