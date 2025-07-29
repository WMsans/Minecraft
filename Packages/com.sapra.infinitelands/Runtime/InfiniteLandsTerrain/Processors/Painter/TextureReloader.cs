using System;

#if UNITY_EDITOR
using UnityEditor;

namespace sapra.InfiniteLands{  
    public class TextureReloader : AssetPostprocessor
    {
        public static Action OnSaveAnyAsset;
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            EditorApplication.delayCall += CallSaveEvent;
        }
        static void CallSaveEvent(){
            OnSaveAnyAsset?.Invoke();
        }
    }
}
#endif