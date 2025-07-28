using UnityEngine;

public class TerrainEditor : MonoBehaviour
{
    public Camera playerCamera;
    public float modificationStrength = 5f;
    public float modificationRadius = 2f;
    public float editCoolDown;
    public byte editType;
    private float lastEditTime;

    void Update()
    {
        if (Input.GetMouseButton(0) && CanEdit()) 
        {
            ModifyTerrain(true);
        }
        else if (Input.GetMouseButton(1) && CanEdit()) 
        {
            ModifyTerrain(false);
        }
    }

    private void ModifyTerrain(bool dig)
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (!OctreeTerrainManager.Instance.Raycast(ray, out var hit)) return;
        float strength = dig ? -modificationStrength : modificationStrength;
        OctreeTerrainManager.Instance.ModifyTerrain(hit.point, strength, modificationRadius, editType);
        lastEditTime = Time.time;
    }

    private bool CanEdit() => Time.time - lastEditTime > editCoolDown;
}