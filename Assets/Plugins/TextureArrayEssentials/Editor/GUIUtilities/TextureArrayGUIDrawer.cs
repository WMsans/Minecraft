using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace TextureArrayEssentials.GUIUtilities
{
    using Compression;

    public class TextureArrayGUIDrawer
    {
        // Array
        private Texture2DArray _array; 
        private string _fileName;

        // Array Settings
        public TextureFormat TextureFormat = TextureFormat.DXT5;
        public bool TransferMipmaps = true;
        public bool ArrayLinear = false; // Not recommended using URP/HDRP as the array will appear brighter

        // Material
        private Object _material;
        private MaterialProperty _arrayProperty;
        private MaterialProperty _assignedTexturesProperty;

        // Texture variables
        private Texture2D[] _textures;
        private int _textureCount;
        private bool[] _assignedTextures;

        /// <summary>
        /// Create a TextureArrayGUIDrawer using the Array and Assigned Textures properties<br />
        /// The Texture2DArray asset will be stored in a folder accompanying the material. Can be moved after creation
        /// </summary>
        /// <param name="arrayProperty">
        /// The material property for the Array (Texture2DArray)
        /// </param>
        /// <param name="assignedTexturesProperty">
        /// The material property for the Assigned Textures (Float)
        /// </param>
        /// <param name="textureCount">
        /// The max amount of textures this array will hold
        /// </param>
        /// <param name="fileName">
        /// The filename of the Texture2DArray asset stored in a folder accompanying the material<br />
        /// Used only on creation of the asset. Array is retrieved from the material afterwards
        /// </param>
        public TextureArrayGUIDrawer(MaterialProperty arrayProperty, MaterialProperty assignedTexturesProperty, int textureCount, string fileName = null)
        {
            // Assign material
            _material = arrayProperty.targets[0];

            // Initialise
            Init(arrayProperty, assignedTexturesProperty, textureCount, fileName);
        }

        /// <summary>
        /// Create a TextureArrayGUIDrawer using a material and property names<br />
        /// The Texture2DArray asset will be stored in a folder accompanying the material. Can be moved after creation
        /// </summary>
        /// <param name="material">
        /// The targeted material
        /// </param>
        /// <param name="arrayPropertyName">
        /// The reference name for the Array (Texture2DArray) shader property
        /// </param>
        /// <param name="assignedTexturesPropertyName">
        /// The reference name for the Assigned Textures (Float) shader property
        /// </param>
        /// <param name="textureCount">
        /// The max amount of textures this array will hold
        /// </param>
        /// <param name="fileName">
        /// The filename of the Texture2DArray asset stored in a folder accompanying the material<br />
        /// Used only on creation of the asset. Can be changed as long as it is assigned in the material.
        /// </param>
        public TextureArrayGUIDrawer(Material material, string arrayPropertyName, string assignedTexturesPropertyName, int textureCount, string fileName = null)
        {
            // Assign material and variables
            _material = material;
            
            // Check if properties are valid
            if (!PropertiesValid(arrayPropertyName, assignedTexturesPropertyName))
                return;

            MaterialProperty arrayProperty = MaterialEditor.GetMaterialProperty(new Object[] { material }, arrayPropertyName);
            MaterialProperty assignedTexturesProperty = MaterialEditor.GetMaterialProperty(new Object[] { material }, assignedTexturesPropertyName);

            // Initialise
            Init(arrayProperty, assignedTexturesProperty, textureCount, fileName);
        }

        /// <summary>
        /// Checks if the Array and Assigned Textures properties are valid
        /// </summary>
        private bool PropertiesValid(string arrayPropertyName, string assignedTexturesPropertyName)
        {
            Material material = (Material)_material;

            if (!material.HasProperty(arrayPropertyName)) {
                Debug.LogError($"Could not find Array property in {_material.name}. Please check shader property name: \"{arrayPropertyName}\"");
                return false;
            }

            if (_arrayProperty.type != MaterialProperty.PropType.Texture || (_arrayProperty.textureValue != null && _arrayProperty.textureValue is not Texture2DArray)) {
                Debug.LogError($"Array property in ({_material.name}) is not a Texture2DArray. Please change the shader property type or check the property name: \"{arrayPropertyName}\"");
                return false;
            }

            if (!material.HasProperty(assignedTexturesPropertyName)) {
                Debug.LogError($"Could not find Assigned Textures property in {_material.name}. Please check shader property name: \"{assignedTexturesPropertyName}\"");
                return false;
            }

            if (_assignedTexturesProperty.type != MaterialProperty.PropType.Float) {
                Debug.LogError($"Assigned Textures property in ({_material.name}) is not a float. Please change the shader property type or check the property name: \"{assignedTexturesPropertyName}\"");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialises this TextureArrayGUIDrawer<br />
        /// Preferably create a new object rather than calling this but it can be called if _material is assigned before.
        /// </summary>
        /// <param name="arrayProperty">
        /// The material property for the Array (Texture2DArray)
        /// </param>
        /// <param name="assignedTexturesProperty">
        /// The material property for the Assigned Textures (Float)
        /// </param>
        /// <param name="textureCount">
        /// The max amount of textures this array will hold
        /// </param>
        /// <param name="fileName">
        /// The filename of the Texture2DArray asset stored in a folder accompanying the material<br />
        /// Used only on creation of the asset. Can be changed as long as it is assigned in the material.
        /// </param>
        private void Init(MaterialProperty arrayProperty, MaterialProperty assignedTexturesProperty, int textureCount, string fileName = null)
        {
            //arrayProperty.type == MaterialProperty.PropType.Texture

            // Assign variables
            _arrayProperty = arrayProperty;
            _assignedTexturesProperty = assignedTexturesProperty;
            _assignedTextures = new bool[textureCount];

            _textureCount = textureCount;
            _textures = new Texture2D[textureCount];

            // Auto assign file name if not set: "TextureArray_ArrayPropertyDisplayName.asset"
            if (fileName == null) {
                string legalDisplayName = string.Join("_", arrayProperty.displayName.Split(System.IO.Path.GetInvalidFileNameChars())); // Remove invalid characters
                legalDisplayName = legalDisplayName.Replace(" ", "_"); // Remove spaces
            
                fileName = $"TextureArray_{legalDisplayName}.asset";
            } else if (!fileName.EndsWith(".asset")) {
                fileName += ".asset";
            }
            
            _fileName = fileName;

            // Load array
            Texture2DArray array = (Texture2DArray)AssetDatabase.LoadAssetAtPath(ArrayPath(), typeof(Texture2DArray));
            
            // If array exists load textures and uncompress assigned textures
            if (array != null) {
                // Get textures from array
                Texture2D[] textures = Texture2DArrayUtilities.GetTextures(array);

                // Get assigned textures
                int compressedAssignedTextures = (int)assignedTexturesProperty.floatValue;
                bool[] assignedTextures = BooleanCompression.GetValues(compressedAssignedTextures, _textures.Length);
                
                // Add textures to array in correct positions
                int currentIndex = 0;
                for (int i = 0; i < assignedTextures.Length; i++) {
                    if (assignedTextures[i]) {
                        _textures[i] = textures[currentIndex];
                        currentIndex++;
                    }
                }
                
                _assignedTextures = assignedTextures;
            }

            _array = array;
        }

        /// <summary>
        /// Updates the texture in the array while handling its order, asset file, and material variables<br />
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="newTexture">
        /// The newly assigned texture
        /// </param>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        private Texture2D UpdateTexture(Texture2D newTexture, int index)
        {
            if (UnityEditor.EditorGUI.EndChangeCheck()) {
                // Return if texture is not being changed, usually due to updates for accompanying variable
                if (newTexture == _textures[index])
                    return newTexture;

                // Register Undo, cannot if material or array are null
                if(_material != null && _array != null)
                    Undo.RegisterCompleteObjectUndo(new Object[] { _material, _array }, $"Modified Array Texture of {_material.name} at Index {index}");

                // Check if texture is assigned or removed
                bool textureAssigned = newTexture != null;

                // If all textures are null, clear array and return
                if (!textureAssigned && _textures.Count(x => x != null) == 1) {
                    DeleteArrayFile();

                    _assignedTextures[index] = textureAssigned;
                    _textures[index] = newTexture;

                    _arrayProperty.textureValue = _array;
                    _assignedTexturesProperty.floatValue = BooleanCompression.CompressValues(_assignedTextures);

                    return newTexture;
                }

                // If texture resolution is not multiple of 4 return
                if (textureAssigned && (newTexture.width % 4 != 0 || newTexture.height % 4 != 0)) {
                    Debug.LogWarning("Cannot update array, Texture resolution must be a multiple of 4");
                    return newTexture;
                }

                // Get the textures in order of the array
                List<Texture2D> arrayTextures = new List<Texture2D>();
                bool[] assignedTexturesArray = new bool[_textures.Length];

                // Get which textures are assigned in inspector and sort it into new list representing array layout
                for (int i = 0; i < _textures.Length; i++) {
                    bool assigned = _textures[i] != null;
                    Texture2D texture = _textures[i];

                    if (i == index) { // Retrieve changed texture at its index
                        assigned = textureAssigned;
                        texture = newTexture;
                    }

                    if (assigned) {
                        arrayTextures.Add(texture);
                    }

                    assignedTexturesArray[i] = assigned;
                }

                if (arrayTextures.Count == 0)
                    return _textures[index];

                // If array is null try loading from material
                if (_array == null) {
                    _array = (Texture2DArray)AssetDatabase.LoadAssetAtPath(ArrayPath(), typeof(Texture2DArray));

                    // If array is still null create a new array and return changed texture
                    if (_array == null) {
                        _textures[index] = newTexture;

                        _array = Texture2DArrayUtilities.CreateArrayUserInput(arrayTextures.ToArray(), TextureFormat, null, TransferMipmaps, ArrayLinear);

                        // Create folder for array
                        string folderPath = AssetDatabase.GetAssetPath(_material);
                        folderPath = folderPath.Substring(0, folderPath.LastIndexOf("/"));

                        if (!AssetDatabase.IsValidFolder($"{folderPath}/{DataFolderName()}"))
                            AssetDatabase.CreateFolder(folderPath, DataFolderName());

                        // Create asset in folder
                        AssetDatabase.CreateAsset(_array, $"{folderPath}/{DataFolderName()}/{_fileName}");

                        // Update variables
                        _assignedTextures[index] = textureAssigned;
                        _textures[index] = newTexture;

                        _arrayProperty.textureValue = _array;
                        _assignedTexturesProperty.floatValue = BooleanCompression.CompressValues(_assignedTextures);

                        return newTexture;
                    }
                }

                // If texture was changed to a different texture update array
                if (_assignedTextures[index] && newTexture != null) {
                    // Get array index by counting how many textures assigned before changed index
                    int arrayIndex = 0;
                    for (int i = 0; i < index; i++) {
                        if (_assignedTextures[i] == true)
                            arrayIndex++;
                    }

                    // Assign texture to array
                    (Texture2DArray, bool) updatedArray = Texture2DArrayUtilities.UpdateTextureUserInput(_array, arrayTextures[arrayIndex], arrayIndex, TransferMipmaps);

                    // If update failed or user cancelled, return unchanged texture
                    if (updatedArray.Item1 == null || updatedArray.Item2) {
                        return Texture2DArrayUtilities.GetTexture(_array, index);
                    }

                    // If array is resized to texture, update file
                    if (_array != updatedArray.Item1)
                        OverwriteArray(updatedArray.Item1);

                    _array = updatedArray.Item1;
                }

                // Otherwise recreate array 
                else {
                    // Automatically resize textures other than the changed one, prevents popups for textures that have already been decided
                    int[] autoResizeIndexes = new int[_textures.Length - 1];

                    int currentIndex = 0;
                    for (int i = 0; i < _textures.Length; i++) {
                        if (i == index) continue;

                        autoResizeIndexes[currentIndex] = i;
                        currentIndex++;
                    }

                    _array = Texture2DArrayUtilities.CreateArrayUserInput(arrayTextures.ToArray(), TextureFormat, autoResizeIndexes, TransferMipmaps, ArrayLinear);
                    OverwriteArray(_array);
                }

                // Save Asset
                EditorUtility.SetDirty(_array);

                // Assign variables
                _arrayProperty.textureValue = _array;

                _assignedTextures[index] = textureAssigned;
                _assignedTexturesProperty.floatValue = BooleanCompression.CompressValues(_assignedTextures);
            }

            // Set texture
            _textures[index] = newTexture;

            return newTexture;
        }

        /// <summary>
        /// Draws a texture using the assigned array
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The texture input into the inspector
        /// </returns>
        public Texture2D DrawTexture(int index, GUIContent content)
        {
            Rect lineRect = GUIUtilities.GetLineRect();
            return DrawTexture(lineRect, index, content);
        }

        /// <summary>
        /// Draws a texture using the assigned array

        /// <summary>
        /// Draws a texture using the assigned array
        /// In some situations will request user input when changing textures with a popup
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The texture input into the inspector
        /// </returns>
        public Texture2D DrawTexture(Rect rect, int index, GUIContent content)
        {
            // Check if valid
            if (_textures == null)
                return null;

            // Draw texture
            UnityEditor.EditorGUI.BeginChangeCheck();
            Texture2D newTexture = GUIUtilities.DrawTexture(rect, _textures[index], content);

            return UpdateTexture(newTexture, index);
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a integer value
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="intValue">
        /// The input integer value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Int Value
        /// </returns>
        public (Texture2D, int) DrawTextureWithInt(int index, int intValue, GUIContent content)
        {
            Rect lineRect = GUIUtilities.GetLineRect();
            return DrawTextureWithInt(lineRect, index, intValue, content);
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a integer value
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="intValue">
        /// The input integer value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Int Value
        /// </returns>
        public (Texture2D, int) DrawTextureWithInt(Rect rect, int index, int intValue, GUIContent content)
        {
            // Check if valid
            if (_textures == null)
                return (null, 0);

            // Draw texture
            UnityEditor.EditorGUI.BeginChangeCheck();
            (Texture2D, int) output = GUIUtilities.DrawTextureWithInt(rect, _textures[index], intValue, content);

            output.Item1 = UpdateTexture(output.Item1, index);

            return output;
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a integer slider
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="sliderValue">
        /// The input slider value
        /// </param>
        /// <param name="sliderMin">
        /// The minimum value that the slider will allow
        /// </param>
        /// <param name="sliderMax">
        /// The maximum value that the slider will allow
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Slider Value
        /// </returns>
        public (Texture2D, int) DrawTextureWithIntSlider(int index, int sliderValue, int sliderMin, int sliderMax, GUIContent content)
        {
            Rect lineRect = GUIUtilities.GetLineRect();
            return DrawTextureWithIntSlider(lineRect, index, sliderValue, sliderMin, sliderMax, content);
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a integer slider
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="sliderValue">
        /// The input slider value
        /// </param>
        /// <param name="sliderMin">
        /// The minimum value that the slider will allow
        /// </param>
        /// <param name="sliderMax">
        /// The maximum value that the slider will allow
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Slider Value
        /// </returns>
        public (Texture2D, int) DrawTextureWithIntSlider(Rect rect, int index, int sliderValue, int sliderMin, int sliderMax, GUIContent content)
        {
            // Check if valid
            if (_textures == null)
                return (null, 0);

            // Draw texture
            UnityEditor.EditorGUI.BeginChangeCheck();
            (Texture2D, int) output = GUIUtilities.DrawTextureWithIntSlider(rect, _textures[index], sliderValue, sliderMin, sliderMax, content);

            output.Item1 = UpdateTexture(output.Item1, index);

            return output;
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a float value
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="floatValue">
        /// The input float value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Float Value
        /// </returns>
        public (Texture2D, float) DrawTextureWithFloat(int index, float floatValue, GUIContent content)
        {
            Rect lineRect = GUIUtilities.GetLineRect();
            return DrawTextureWithFloat(lineRect, index, floatValue, content);
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a float value
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="floatValue">
        /// The input float value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Float Value
        /// </returns>
        public (Texture2D, float) DrawTextureWithFloat(Rect rect, int index, float floatValue, GUIContent content)
        {
            // Check if valid
            if (_textures == null)
                return (null, 0.0f);

            // Draw texture
            UnityEditor.EditorGUI.BeginChangeCheck();
            (Texture2D, float) output = GUIUtilities.DrawTextureWithFloat(rect, _textures[index], floatValue, content);

            output.Item1 = UpdateTexture(output.Item1, index);

            return output;
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a slider
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="sliderValue">
        /// The input slider value
        /// </param>
        /// <param name="sliderMin">
        /// The minimum value that the slider will allow
        /// </param>
        /// <param name="sliderMax">
        /// The maximum value that the slider will allow
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Slider Value
        /// </returns>
        public (Texture2D, float) DrawTextureWithSlider(int index, float sliderValue, float sliderMin, float sliderMax, GUIContent content)
        {
            Rect lineRect = GUIUtilities.GetLineRect();
            return DrawTextureWithSlider(lineRect, index, sliderValue, sliderMin, sliderMax, content);
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a slider
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="sliderValue">
        /// The input slider value
        /// </param>
        /// <param name="sliderMin">
        /// The minimum value that the slider will allow
        /// </param>
        /// <param name="sliderMax">
        /// The maximum value that the slider will allow
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Slider Value
        /// </returns>
        public (Texture2D, float) DrawTextureWithSlider(Rect rect, int index, float sliderValue, float sliderMin, float sliderMax, GUIContent content)
        {
            // Check if valid
            if (_textures == null)
                return (null, 0.0f);

            // Draw texture
            UnityEditor.EditorGUI.BeginChangeCheck();
            (Texture2D, float) output = GUIUtilities.DrawTextureWithSlider(rect, _textures[index], sliderValue, sliderMin, sliderMax, content);

            output.Item1 = UpdateTexture(output.Item1, index);

            return output;
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a color value
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="colorValue">
        /// The input color value
        /// </param>
        /// <param name="hdr">
        /// If the color is HDR
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Color Value
        /// </returns>
        public (Texture2D, Color) DrawTextureWithColor(int index, Color colorValue, bool hdr, GUIContent content)
        {
            Rect lineRect = GUIUtilities.GetLineRect();
            return DrawTextureWithColor(lineRect, index, colorValue, hdr, content);
        }

        /// <summary>
        /// Draws a texture using the assigned array along with a color value
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="index">
        /// Index of the texture being changed in the desired array layout<br />
        /// Not the index of the current array layout or textures, think of it as a constant within the set texture count
        /// </param>
        /// <param name="colorValue">
        /// The input color value
        /// </param>
        /// <param name="hdr">
        /// If the color is HDR
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// Item1: Texture, Item2: Color Value
        /// </returns>
        public (Texture2D, Color) DrawTextureWithColor(Rect rect, int index, Color colorValue, bool hdr, GUIContent content)
        {
            // Check if valid
            if (_textures == null)
                return (null, Color.white);

            // Draw texture
            UnityEditor.EditorGUI.BeginChangeCheck();
            (Texture2D, Color) output = GUIUtilities.DrawTextureWithColor(rect, _textures[index], colorValue, hdr, content);

            output.Item1 = UpdateTexture(output.Item1, index);

            return output;
        }

        /// <summary>
        /// Draws all the textures in the array using DrawTexture
        /// In some situations this will request user input when changing textures with a popup
        /// </summary>
        /// <param name="content">
        /// The GUIContent for each texture field in order<br />
        /// If unassigned each texture will be named "Texture 1", "Texture 2", ...
        /// </param>
        /// <returns>
        /// Array of textures input into the inspector
        /// </returns>
        public Texture2D[] DrawTextures(GUIContent[] content = null)
        {
            // Check if valid
            if (_textures == null)
                return null;

            // Draw textures
            Texture2D[] textures = new Texture2D[_textureCount];

            for (int i = 0; i < _textureCount; i++) {
                // Create content if not assigned
                GUIContent currentContent = new GUIContent($"Texture {i + 1}");
                if (content != null && content[i] != null)
                    currentContent = content[i];

                // Draw textures
                textures[i] = DrawTexture(i, currentContent);
            }

            return textures;
        }

        /// <summary>
        /// Returns if a texture is assigned at the given index
        /// </summary>
        /// <param name="index">
        /// The index of the texture being checked
        /// </param>
        /// <returns>
        /// If the texture at the given index is assigned in the inspector
        /// </returns>
        public bool TextureAssignedAt(int index)
        {
            return _assignedTextures[index];
        }

        /// <summary>
        /// Gets the path of the assigned array in the material
        /// </summary>
        private string ArrayPath()
        {
            return AssetDatabase.GetAssetPath(_arrayProperty.textureValue);
        }

        /// <summary>
        /// Gets the folder name of where the arrays for this material are stored
        /// </summary>
        /// <returns></returns>
        private string DataFolderName()
        {
            string path = AssetDatabase.GetAssetPath(_material);
            int lastIndex = path.LastIndexOf("/");
            string fileName = path.Substring(lastIndex + 1, path.Length - lastIndex - 1).Split(".")[0];

            return fileName + "_TextureData";
        }

        /// <summary>
        /// Overwrites the array with the input deleting the previous and replacing it with the new one
        /// </summary>
        /// <param name="array">
        /// Array that will overwrite the current
        /// </param>
        private void OverwriteArray(Texture2DArray array)
        {
            string path = ArrayPath();
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(array, path);
        }

        /// <summary>
        /// Clears the array and deletes its file and folder if empty
        /// </summary>
        public void DeleteArray()
        {
            DeleteArrayFile();

            _textures = new Texture2D[_textureCount];
            _assignedTextures = new bool[_textureCount];

            _arrayProperty.textureValue = _array;
            _assignedTexturesProperty.floatValue = BooleanCompression.CompressValues(_assignedTextures);
        }

        /// <summary>
        /// Deletes the array file and folder if empty<br />
        /// Does not prevent drawing further textures, acts more like a reset
        /// </summary>
        private void DeleteArrayFile()
        {
            string arrayPath = ArrayPath();

            // Delete array if it exists
            if (System.IO.File.Exists(arrayPath)) {
                AssetDatabase.DeleteAsset(arrayPath);
                for (int i = 0; i < _textures.Length; i++) {
                    _textures[i] = null;
                }
            }

            // If data folder is empty, delete it
            string folderPath = AssetDatabase.GetAssetPath(_material);
            folderPath = folderPath.Substring(0, folderPath.LastIndexOf("/"));
            folderPath = $"{folderPath}/{DataFolderName()}";

            if (AssetDatabase.IsValidFolder(folderPath)) {
                bool empty = !Directory.EnumerateFiles(folderPath).Any();
                if (empty) AssetDatabase.DeleteAsset(folderPath);
            }
        }
    }
}
#endif