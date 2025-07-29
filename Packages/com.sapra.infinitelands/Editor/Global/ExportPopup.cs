using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
using UnityEditor.UIElements;
using System;
using System.Linq;

namespace sapra.InfiniteLands.Editor{
    public class ExportPopup : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        private IGraph generator;
        private GraphSettings settings;

        private int CurrentQualityIndex;
       
        FloatField ScaleField;
        Vector2Field WorldOffsetField;
        IntegerField SeedField;
        ObjectField GeneratorField;
        DropdownField QualityField;
        DropdownField ExportMode;
        Button cancel;   
        Button export;
        Label Description;

        Type SelectedType;
        public static void OpenPopup(IGraph generator, GraphSettings settings)
        {
            ExportPopup wnd = GetWindow<ExportPopup>();
            wnd.titleContent = new GUIContent("Export data");
            wnd.generator = generator;
            wnd.settings = settings;
            wnd.UpdateVisualElements();
        }

        [MenuItem("Window/Infinite Lands/Export Data", false, 5010)]
        public static void OpenPopup()
        {
            ExportPopup wnd = GetWindow<ExportPopup>();
            wnd.titleContent = new GUIContent("Export data");
            wnd.settings = GraphSettingsController.GetSettings();
            wnd.UpdateVisualElements();
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            m_VisualTreeAsset.CloneTree(rootVisualElement);

            ScaleField = root.Q<FloatField>("scale");
            WorldOffsetField = root.Q<Vector2Field>("position");
            SeedField = root.Q<IntegerField>("seed");
            GeneratorField = root.Q<ObjectField>("generator");
            QualityField = root.Q<DropdownField>("quality");
            ExportMode = root.Q<DropdownField>("exporter");
            Description = root.Q<Label>("description");

            cancel = root.Q<Button>("cancel");            
            export = root.Q<Button>("export");

            cancel.clicked += Close;
            export.clicked += Export;
        }

        public void UpdateVisualElements(){
            if(settings == null)
                settings = GraphSettingsController.GetSettings();
            SerializedObject serializedObject = new SerializedObject(settings);

            export.SetEnabled(generator != null);

            Type[] exportableTypes = GetAllExportableTypes();
            ExportMode.choices.Clear();
            SelectedType = exportableTypes[0];
            foreach(Type type in exportableTypes){
                ExportMode.choices.Add(type.Name);
            }
            ExportMode.value = ExportMode.choices[0];
            ExportMode.RegisterValueChangedCallback(a => {
                Description.text = (Activator.CreateInstance(exportableTypes[ExportMode.index]) as IExportTextures).description;
                SelectedType = exportableTypes[ExportMode.index];}
            );

            Description.text = (Activator.CreateInstance(exportableTypes[0]) as IExportTextures).description;

            var bj = generator as UnityEngine.Object;
            
            if(bj != null)
                GeneratorField.value = bj;
            GeneratorField.RegisterValueChangedCallback( a =>{
                var bj = a.newValue as IGraph;
                generator = bj;
                export.SetEnabled(bj != null);
            });
            
            int starter = 32;
            QualityField.choices.Add(32.ToString());
            for(int i = 0; i < 9; i++){
                starter += starter;
                QualityField.choices.Add(starter.ToString());
            }
            QualityField.value = 512.ToString();
            QualityField.RegisterValueChangedCallback( a=> CurrentQualityIndex = QualityField.index);


            ScaleField.bindingPath = nameof(settings.MeshScale);
            ScaleField.isDelayed = true;
            ScaleField.RegisterValueChangedCallback(a =>
            {
                float value = Mathf.Max(100, a.newValue);
                ScaleField.SetValueWithoutNotify(value);
            });

            WorldOffsetField.bindingPath = nameof(settings.WorldOffset);
            SeedField.bindingPath = nameof(settings.Seed);


            ScaleField.Bind(serializedObject);
            SeedField.Bind(serializedObject);
            WorldOffsetField.Bind(serializedObject);
        }
        Type[] GetAllExportableTypes(){
            var types = TypeCache.GetTypesDerivedFrom<IExportTextures>().Where(a => !a.IsGenericType).ToArray();
            return types;
        }
        private void Export(){
            if(SelectedType == null){
                Debug.LogError("Please select an Export Mode before exporing data");
                return;
            }
            int EditorResolution = (int)Mathf.Pow(2, CurrentQualityIndex+5);
            IExportTextures exporter = Activator.CreateInstance(SelectedType) as IExportTextures;
            exporter.SetExporterResolution(EditorResolution);
            
            ExporterGenerator newGenerator = new ExporterGenerator(generator);
            List<Texture2D> results = newGenerator.GenerateAndExportWorld(EditorResolution, settings.MeshScale, settings.Seed, settings.WorldOffset, exporter);
            foreach(Texture2D texture in results){
                SaveTexture(generator, texture, EditorResolution, settings.MeshScale, settings.WorldOffset, settings.Seed);
            }
            exporter.DestroyTextures(DestroyImmediate);
        }

        static void SaveTexture(IGraph tree, Texture2D texture, int resolution, float scale, Vector2 offset, int seed){
            byte[] bytes = texture.EncodeToPNG();
            if(!Directory.Exists(Application.dataPath+"/Exports/"+tree.name))
                Directory.CreateDirectory(Application.dataPath+"/Exports/"+tree.name);

            File.WriteAllBytes(Application.dataPath + "/Exports/" + string.Format("{0}/{5}-scale{1}reso{2}pos{3}seed{4}", tree.name, scale, resolution, offset.ToString(), seed, texture.name)+".png", bytes);
            AssetDatabase.Refresh();
        }
    }
}