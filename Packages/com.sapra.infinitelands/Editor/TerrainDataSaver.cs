using UnityEditor;
using UnityEngine;
using System.IO;

namespace sapra.InfiniteLands.Editor{
    public static class TerrainDataSaver
    {
        public static void SaveTerrainData(TerrainData originalData, string generatorName)
        {
            // Define the save path
            string exportFolder = string.Format("{0}/Exports/{1}", Application.dataPath, generatorName);
            if (!Directory.Exists(exportFolder))
                Directory.CreateDirectory(exportFolder);

            string relativeFolder = "Assets" + exportFolder.Substring(Application.dataPath.Length);
            string filePath = string.Format("{0}/{1}.asset", relativeFolder, originalData.name);

            // Create or load the TerrainData asset
            TerrainData newTerrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(filePath);
            if (newTerrainData == null)
            {
                newTerrainData = Object.Instantiate(originalData);
                AssetDatabase.CreateAsset(newTerrainData, filePath);
            }

            // Save and refresh the Asset Database
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Save terrain layers and alpha maps
            newTerrainData.terrainLayers = SaveTerrainLayers(originalData.terrainLayers, relativeFolder);
            newTerrainData.SetAlphamaps(0, 0, originalData.GetAlphamaps(0, 0, originalData.alphamapWidth, originalData.alphamapHeight));

            // Save tree prototypes and instances
            newTerrainData.treePrototypes = SaveTreePrototypes(originalData.treePrototypes, relativeFolder);

            newTerrainData.SetTreeInstances(originalData.treeInstances, true);

            // Final asset save and refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Assign the saved TerrainData back to the terrain
            Debug.Log($"Terrain data saved successfully at {filePath}");
        }

        private static TreePrototype[] SaveTreePrototypes(TreePrototype[] originalPrototypes, string relativeFolder)
        {
            TreePrototype[] newPrototypes = new TreePrototype[originalPrototypes.Length];

            for (int i = 0; i < originalPrototypes.Length; i++)
            {
                TreePrototype prototype = originalPrototypes[i];

                // Create a unique path for the tree prototype prefab
                string treePath = string.Format("{0}/TreePrototype_{1}.prefab", relativeFolder, i);
                TreePrototype newPrototype = SaveTreePrototype(prototype, treePath);

                // Assign the saved prototype to the new array
                newPrototypes[i] = newPrototype;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return newPrototypes;
        }

        private static TreePrototype SaveTreePrototype(TreePrototype originalPrototype, string treePath)
        {
            // Check if the tree prefab already exists
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePath);
            if (treePrefab == null)
            {
                // If not, create a new prefab
                treePrefab = PrefabUtility.SaveAsPrefabAsset(originalPrototype.prefab, treePath);
                treePrefab.name = originalPrototype.prefab.name;
            }

            // Return a new tree prototype with the saved prefab
            TreePrototype newProtoype = new TreePrototype(originalPrototype);
            newProtoype.prefab = treePrefab;
            return newProtoype;
        }

        private static TerrainLayer[] SaveTerrainLayers(TerrainLayer[] originalLayers, string relativeFolder)
        {
            TerrainLayer[] newLayers = new TerrainLayer[originalLayers.Length];

            for (int i = 0; i < originalLayers.Length; i++)
            {
                TerrainLayer layer = originalLayers[i];

                // Create a unique path for the terrain layer asset
                string layerPath = string.Format("{0}/TerrainLayer_{1}.terrainlayer", relativeFolder, i);
                TerrainLayer newLayer = SaveTerrainLayer(layer, layerPath);

                // Assign the saved layer to the new array
                newLayers[i] = newLayer;
            }

            return newLayers;
        }

        private static TerrainLayer SaveTerrainLayer(TerrainLayer originalLayer, string layerPath)
        {
            // Check if the terrain layer already exists
            TerrainLayer layerAsset = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layerAsset == null)
            {
                // If not, create a new terrain layer
                layerAsset = new TerrainLayer();
                layerAsset.tileSize = originalLayer.tileSize;
                layerAsset.diffuseTexture = originalLayer.diffuseTexture;
                layerAsset.normalMapTexture = originalLayer.normalMapTexture;
                layerAsset.maskMapTexture = originalLayer.maskMapTexture;

                AssetDatabase.CreateAsset(layerAsset, layerPath);
            }

            // Return the saved terrain layer
            return layerAsset;
        }
    }
}