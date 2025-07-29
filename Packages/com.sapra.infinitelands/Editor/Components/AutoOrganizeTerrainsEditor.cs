using UnityEngine;
using UnityEditor;

namespace sapra.InfiniteLands.Editor{
    [CustomEditor(typeof(AutoOrganizeTerrains))]
    public class AutoOrganizeTerrainsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Place terrains to correct place")){
                AutoOrganizeTerrains chunk = target as AutoOrganizeTerrains;
                if (chunk) 
                    chunk.OrganizeTerrains();
            }

        }
    }
}
