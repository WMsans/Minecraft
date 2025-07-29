/* using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    [CustomEditor(typeof(SplatMapVisualizer))]
    [CanEditMultipleObjects]
    
    public class SplatMapVisualizerEditor : UnityEditor.Editor
    {
        private GUIStyle highlightButton;
        private GUIStyle defaultButton;

        public override void OnInspectorGUI()
        {
            if(highlightButton == null){
                highlightButton = new GUIStyle(GUI.skin.GetStyle("Button"));
                highlightButton.fontStyle = FontStyle.Bold;
            }

            if(defaultButton == null){
                defaultButton = new GUIStyle(GUI.skin.GetStyle("Button"));
            }
            base.OnInspectorGUI();
            var painter = target as SplatMapVisualizer;
            IEnumerable<InfiniteLandsAsset> allAssets = painter.Assets;

            if(allAssets == null)
                return;
                
            if(painter.ProceduralTexturing)
                GUI.color = Color.white;
            else
                GUI.color = Color.gray;
            if(GUILayout.Button("Procedural", painter.ProceduralTexturing ?highlightButton : defaultButton)){
                painter.ChangeToTexture(default, true);
            }
            GUI.color = Color.gray;

            if(!painter.ProceduralTexturing && painter.DesiredSplatMapAsset == null)
                GUI.color = Color.white;
            if(GUILayout.Button("Normal Map", !painter.ProceduralTexturing && painter.DesiredSplatMapAsset == null ? highlightButton : defaultButton)){
                painter.ChangeToTexture(default, false);
            }
            GUI.color = Color.gray;

            var grouped = allAssets.GroupBy(a => a.GetType());
            foreach(var asset in grouped){
                GUI.color = Color.white;
                EditorGUILayout.LabelField(asset.Key.Name);
                GUI.color = Color.gray;

                foreach(InfiniteLandsAsset value in asset){
                    if(painter.DesiredSplatMapAsset != null && painter.DesiredSplatMapAsset.Equals(value))
                        GUI.color = Color.white;
                    if(GUILayout.Button(value.name, painter.DesiredSplatMapAsset != null && painter.DesiredSplatMapAsset.Equals(value) ? highlightButton : defaultButton)){
                        painter.ChangeToTexture(value, false);
                    }
                    GUI.color = Color.gray;

                }
            }
        }
    }
} */