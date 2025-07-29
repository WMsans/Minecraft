using System;
using System.Linq;
using UnityEditor;

namespace sapra.InfiniteLands.Editor{
    public class InfiniteLandsPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if(importedAssets.Contains("Packages/com.sapra.infinitelands")){
                AboutWindow.OpenPopup();
            }

            foreach(string path in movedAssets){
                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                if(obj is TerrainGenerator generator){
                    InfiniteLandsWindow.ReloadWindow(generator);
                }
            }
        }
    }

    public class InifniteLandsModificationProcessor : AssetModificationProcessor{
        static AssetDeleteResult OnWillDeleteAsset(string path,RemoveAssetOptions opt){
            UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
            if(obj is TerrainGenerator generator){
                InfiniteLandsWindow.CloseWindow(generator);
            }
            return AssetDeleteResult.DidNotDelete;
        }   
    }
}