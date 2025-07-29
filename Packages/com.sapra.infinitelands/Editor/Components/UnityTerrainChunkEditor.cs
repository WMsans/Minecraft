using UnityEngine;
using UnityEditor;
using sapra.InfiniteLands.UnityTerrain;


namespace sapra.InfiniteLands.Editor{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityTerrainChunk))]
    public class UnityTerrainChunkEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI(){
            base.OnInspectorGUI();
            UnityTerrainChunk chunk = target as UnityTerrainChunk;
            TerrainData data = chunk.terrain?.terrainData;
            if (GUILayout.Button("Save this Terrain")){
                if (chunk) 
                    SaveTerrain(data, chunk.GetGraphName());
            }

            if (GUILayout.Button("Save all Terrains")){
                if (chunk) {
                    SaveTerrain(data, chunk.GetGraphName());
                    SaveOtherTerrains(false);
                }
            }

            if (GUILayout.Button("Save visible Terrains")){
                if (chunk) {
                    SaveTerrain(data, chunk.GetGraphName());
                    SaveOtherTerrains(true);
                }
            }
        }

        public void SaveTerrain(TerrainData terrainData, string name){
            if(terrainData){
                TerrainDataSaver.SaveTerrainData(terrainData, name);
                
            }
        }

        public void SaveOtherTerrains(bool visible){
            UnityTerrainChunk[] allTheChunks = FindObjectsByType<UnityTerrainChunk>(FindObjectsSortMode.None);
            foreach(UnityTerrainChunk chunk in allTheChunks){
                if(!chunk.isActiveAndEnabled && visible)
                    continue;
                TerrainData data = chunk.terrain?.terrainData;
                SaveTerrain(data, chunk.GetGraphName());
            }
        }
    }
}