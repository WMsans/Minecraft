using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;

namespace TextureArrayEssentials.CustomWindows
{
    using GUIUtilities;

    public class Texture2DArrayWindowBase : EditorWindow
    {
        // Supported texture format names
        private string[] _supportedTextureFormatNames;

        // Textures
        protected List<Texture2D> _textures;
        protected List<bool> _texturesResizing;
        protected List<bool> _textureErrors;

        // Array Settings
        protected int _arrayTextureFormatIndex;
        protected bool _arrayMipMaps;
        protected bool _arrayLinear;

        protected TextureWrapMode _arrayWrapMode;
        protected FilterMode _arrayFilterMode;
        protected int _arrayAnisoLevel;

        protected bool _resizeTextures;
        protected Vector2Int _arrayResolution;
        protected FilterMode _resizeFilterMode;
        private Vector2Int _previousResizeResolution;

        // Array Settings toggles for child classes
        protected bool _textureFormatSettingEnabled = true;
        protected bool _mipmapsSettingEnabled = true;
        protected bool _linearSettingEnabled = true;
        protected bool _wrapModeSettingEnabled = true;
        protected bool _filterModeSettingEnabled = true;
        protected bool _anisoLevelSettingEnabled = true;
        protected bool _resizeSettingEnabled = true;

        // GUI
        private ReorderableList _textureROList;

        private Vector2 _scrollPosition = new Vector2(0, 0);

        protected GUIStyle _headerStyle;

        /// <summary>
        /// Called when theGUI is first created<br />
        /// base.CreateGUI(); Must be called before performing operations
        /// </summary>
        protected virtual void CreateGUI()
        {
            // Cache texture format names instead of creating each GUI call
            _supportedTextureFormatNames = Texture2DArrayUtilities.SUPPORTED_TEXTURE_FORMATS.Select(x => System.Enum.GetName(typeof(TextureFormat), x)).ToArray();

            // Setup texture variables before ReorderableList
            _textures = new List<Texture2D>();
            _texturesResizing = new List<bool>();
            _textureErrors = new List<bool>();

            for (int i = 0; i < 2; i++) {
                _textures.Add(null);
                _texturesResizing.Add(false);
                _textureErrors.Add(false);
            }

            // Setup textures GUI
            _textureROList = new ReorderableList(_textures, typeof(Texture2D), true, false, true, true);

            _textureROList.drawElementCallback = DrawTextureField;
            _textureROList.elementHeightCallback = CalculateElementHeight;
            _textureROList.onAddCallback = OnTextureAdd;
            _textureROList.onRemoveCallback = OnTextureRemove;
            _textureROList.onReorderCallbackWithDetails = OnTexturesReorder;

            // Reset variables
            _arrayTextureFormatIndex = System.Array.IndexOf(Texture2DArrayUtilities.SUPPORTED_TEXTURE_FORMATS, TextureFormat.DXT5);
            _arrayMipMaps = true;
            _arrayLinear = false;

            _arrayWrapMode = TextureWrapMode.Repeat;
            _arrayFilterMode = FilterMode.Bilinear;
            _arrayAnisoLevel = 1;

            _resizeTextures = false;
            _arrayResolution = new Vector2Int();
            _resizeFilterMode = FilterMode.Bilinear;
            _previousResizeResolution = Vector2Int.zero;
        }

        private void OnGUI()
        {
            // Allow scroll incase of overflow
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            // Update style in OnGUI in case of compiling, will break otherwise
            _headerStyle = new GUIStyle("label");
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.fontSize = 16;

            GUILayout.Space(10);

            OnGUIUpdate();

            GUILayout.EndScrollView();

            // Handle DragAndDrop at end to prevent updates when changing individual textures
            switch (Event.current.type) {
                // Set visual mode only when dragging texture
                case EventType.DragUpdated: {
                    OnDragUpdate(DragAndDrop.objectReferences);

                    Event.current.Use();
                    break;
                }
                // Add all dropped textures to the list
                case EventType.DragPerform: {
                    DragAndDrop.AcceptDrag();

                    OnDragPerform(DragAndDrop.objectReferences);

                    Event.current.Use();
                    break;
                }
            }
        }

        /// <summary>
        /// Called every GUI update, used due to other tasks in OnGUI<br />
        /// base.OnGUI(); Will create the textures, settings, and output sections
        /// </summary>
        protected virtual void OnGUIUpdate()
        {
            GUILayout.Label("Textures", _headerStyle);

            // Draw textures
            GUILayout.BeginHorizontal();
            GUILayout.Space(3);

            if(_textureROList != null)
                _textureROList.DoLayoutList();

            GUILayout.Space(3);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Changable Array Settings
            GUILayout.Label("Array Settings", _headerStyle);

            GUIUtilities.BeginBackgroundVertical();

            DrawArraySettings();

            GUIUtilities.EndBackgroundVertical();
            GUILayout.Space(10);

            // Final settings for the output array
            GUILayout.Label("Final Output", _headerStyle);

            GUIUtilities.BeginBackgroundVertical();

            DrawFinalOutputFields();
            DisplayWarnings();

            GUIUtilities.EndBackgroundVertical();
        }

        /// <summary>
        /// Calculates the height of an element for the ReorderableList GUI where the textures are assigned<br />
        /// base.CalculateElementHeight(); Must be called before performing operations
        /// </summary>
        /// <param name="index">
        /// Index of the texture list which the height is being calculated for the ReorderableList GUI
        /// </param>
        /// <returns>
        /// Height that the element will use
        /// </returns>
        protected virtual float CalculateElementHeight(int index)
        {
            float elementHeight = GUIUtilities.LINE_HEIGHT + GUIUtilities.LINE_SPACING;

            // Room for texture details
            if (_textures[index] != null)
                elementHeight += (GUIUtilities.LINE_HEIGHT + GUIUtilities.LINE_SPACING) * 2;

            // Room for error help box
            if (_textureErrors[index])
                elementHeight += GUIUtilities.LINE_HEIGHT * 3 + GUIUtilities.LINE_SPACING * 4;

            return elementHeight;
        }

        /// <summary>
        /// Draws a texture element in the textures list<br />
        /// When modifying the height in any way Repaint(); should be called to update it on the same GUI call, otherwise will be updated on the next
        /// </summary>
        /// <param name="rect">
        /// Rect with all the available space in the element<br />
        /// Available space is calculated and can be changed in CalculateElementHeight
        /// </param>
        /// <param name="index">
        /// The index of the current element in the textures list
        /// </param>
        /// <param name="isActive">
        /// If this element is is being moved or interacted with in the GUI
        /// </param>
        /// <param name="isFocused">
        /// If this element is selected in the GUI
        /// </param>
        protected virtual void DrawTextureField(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _textures.Count)
                return;

            rect.height = GUIUtilities.LINE_HEIGHT;
            rect.y += GUIUtilities.LINE_SPACING;

            // Texture
            EditorGUI.BeginChangeCheck();
            Rect texRect = rect;
            Texture2D texture = GUIUtilities.DrawTexture(texRect, _textures[index], new GUIContent($"Texture {index + 1}"));
            _textures[index] = texture;

            if (EditorGUI.EndChangeCheck()) {
                _texturesResizing[index] = false;

                // Set resolution if not already set
                if (_arrayResolution == Vector2Int.zero && texture != null)
                    _arrayResolution = new Vector2Int(texture.width, texture.height);
            }

            _textures[index] = texture;

            if (texture == null)
                return;

            // Resolution
            Rect resRect = rect;
            resRect.y += GUIUtilities.LINE_HEIGHT + GUIUtilities.LINE_SPACING;
            EditorGUI.LabelField(resRect, $"Resolution: {texture.width}x{texture.height}{((_texturesResizing[index] || (_resizeTextures && (texture.width != _arrayResolution.x || texture.height != _arrayResolution.y))) ? $" > {_arrayResolution.x}x{_arrayResolution.y}" : "")}", EditorStyles.boldLabel);

            // Format
            Rect formatRect = rect;
            formatRect.y += (GUIUtilities.LINE_HEIGHT + GUIUtilities.LINE_SPACING) * 2;
            EditorGUI.LabelField(formatRect, $"Format: {texture.format}{(texture.format != Texture2DArrayUtilities.SUPPORTED_TEXTURE_FORMATS[_arrayTextureFormatIndex] ? $" > {Texture2DArrayUtilities.SUPPORTED_TEXTURE_FORMATS[_arrayTextureFormatIndex]}" : "")}", EditorStyles.boldLabel);

            // If resolution is different to array, prompt user to fix it
            if (!_resizeTextures && !_texturesResizing[index] && (texture.width != _arrayResolution.x || texture.height != _arrayResolution.y)) {
                // Help Box
                Rect helpBoxRect = rect;
                helpBoxRect.y += (GUIUtilities.LINE_HEIGHT + GUIUtilities.LINE_SPACING) * 3;
                helpBoxRect.height = (GUIUtilities.LINE_HEIGHT + GUIUtilities.LINE_SPACING) * 2;
                EditorGUI.HelpBox(helpBoxRect, $"{_arrayResolution.x}x{_arrayResolution.y} != {texture.width}x{texture.height}\nMismatched resolution, please decide what to do with the texture", MessageType.Error);

                // Resize Texture
                Rect leftButtonRect = rect;
                leftButtonRect.y += GUIUtilities.LINE_HEIGHT * 5 + GUIUtilities.LINE_SPACING * 6;
                leftButtonRect.width /= 2;
                leftButtonRect.width -= GUIUtilities.LINE_SPACING;

                if (GUI.Button(leftButtonRect, "Resize Texture")) {
                    _texturesResizing[index] = true;
                }

                // Resize Array
                Rect rightButtonRect = leftButtonRect;
                rightButtonRect.x += leftButtonRect.width + GUIUtilities.LINE_SPACING * 2;

                if (GUI.Button(rightButtonRect, "Resize Array")) {
                    _arrayResolution = new Vector2Int(texture.width, texture.height);

                    // Set all other textures to resize if they require it
                    for (int i = 0; i < _texturesResizing.Count; i++) {
                        if (i == index) {
                            _texturesResizing[i] = false;
                            continue;
                        }

                        bool resizing = true;
                        if (texture.width == _arrayResolution.x && texture.height == _arrayResolution.y)
                            resizing = false;

                        _texturesResizing[i] = resizing;
                    }
                }

                if (_textureErrors.Count > index) {
                    _textureErrors[index] = true;
                    Repaint(); // Update element height if error is set, otherwise will be set next update
                }

            } else if (_textureErrors.Count > index) {
                _textureErrors[index] = false;
                Repaint(); // Update element height if error is set, otherwise will be set next update
            }
        }

        /// <summary>
        /// Draws the settings of the output array. Settings that are enabled here can be toggles with the respective settingEnabled variables<br />
        /// All GUI called here will be drawn within the Array Settings section<br />
        /// base.DrawArraySettings() will draw the settings
        /// </summary>
        protected virtual void DrawArraySettings()
        {
            // Settings
            if (_textureFormatSettingEnabled) _arrayTextureFormatIndex = EditorGUILayout.Popup("Texture Format", _arrayTextureFormatIndex, _supportedTextureFormatNames);
            if (_mipmapsSettingEnabled) _arrayMipMaps = GUILayout.Toggle(_arrayMipMaps, new GUIContent("Generate Mipmaps"));
            if (_linearSettingEnabled) _arrayLinear = GUILayout.Toggle(_arrayLinear, new GUIContent("Linear"));
            if (_wrapModeSettingEnabled) _arrayWrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", _arrayWrapMode);
            if (_filterModeSettingEnabled) _arrayFilterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", _arrayFilterMode);
            if (_anisoLevelSettingEnabled) _arrayAnisoLevel = EditorGUILayout.IntSlider("Aniso Level", _arrayAnisoLevel, 0, 16);

            // Resize Textures
            if (_resizeSettingEnabled) {
                UnityEditor.EditorGUI.BeginChangeCheck();
                _resizeTextures = GUILayout.Toggle(_resizeTextures, new GUIContent("Resize Textures"));
                if (UnityEditor.EditorGUI.EndChangeCheck()) {
                    // Store/Retrieve resolution before overriding
                    if (_resizeTextures)
                        _previousResizeResolution = _arrayResolution;
                    else
                        _arrayResolution = _previousResizeResolution;
                }

                // If overriding resolution draw resolution input field below
                if (_resizeTextures) {
                    // Draw resolution field
                    UnityEditor.EditorGUI.BeginChangeCheck();

                    Vector2Int prevResolution = _arrayResolution;
                    _arrayResolution = GUIUtilities.DrawVector2IntField(_arrayResolution, new GUIContent("Resolution"));

                    if (UnityEditor.EditorGUI.EndChangeCheck()) {
                        // Must be multiple of 4, round to closest
                        if (_arrayResolution.x % 4 != 0 || _arrayResolution.y % 4 != 0) {
                            // If value is lower than previous round down, otherwise round up. Causes inconsistencies with mouse dragging otherwise
                            bool lower = _arrayResolution.x < prevResolution.x || _arrayResolution.y < prevResolution.y;
                            _arrayResolution.x = lower ? ((_arrayResolution.x / 4) * 4) : (((_arrayResolution.x + 3) / 4) * 4);
                            _arrayResolution.y = lower ? ((_arrayResolution.y / 4) * 4) : (((_arrayResolution.y + 3) / 4) * 4);
                        }
                    }
                }
            }

            // If resizing any textures display resize filter mode setting
            if (_resizeTextures || _texturesResizing.Any(x => x == true)) {
                _resizeFilterMode = (FilterMode)EditorGUILayout.EnumPopup("Resize Filter Mode", _resizeFilterMode);
            }
        }

        /// <summary>
        /// Draws the details of the output array<br />
        /// All GUI called here will be drawn within the Final Output section<br />
        /// base.DrawFinalOutputFields() will draw the final output details
        /// </summary>
        protected virtual void DrawFinalOutputFields()
        {
            // Array depth = amount of textures assigned
            int arrayTextureDepth = 0;
            for (int i = 0; i < _textures.Count; i++) {
                if (_textures[i] != null)
                    arrayTextureDepth++;
            }

            GUILayout.Label($"Resolution: {_arrayResolution.x}x{_arrayResolution.y}");
            GUILayout.Label($"Depth: {arrayTextureDepth}");
            GUILayout.Label($"Format: {Texture2DArrayUtilities.SUPPORTED_TEXTURE_FORMATS[_arrayTextureFormatIndex]}");
        }

        /// <summary>
        /// Displays any warnings for array creation<br />
        /// All GUI called here will be drawn within and at the bottom of the Final Output section<br />
        /// base.DrawFinalOutputFields() must be called before showing any extra warnings
        /// </summary>
        protected virtual void DisplayWarnings()
        {
            // If using linear on non-birp display warning
            if(_arrayLinear && GraphicsSettings.defaultRenderPipeline != null) {
                EditorGUILayout.HelpBox("Linear set to enabled while in URP/HDRP will result in a brighter texture output", MessageType.Warning);
            }

            // If using mipmaps display warnings for some resolutions
            if (_arrayMipMaps) {
                // If resolution is not a power of 2, mipmaps will break
                if (((_arrayResolution.x & (_arrayResolution.x - 1)) != 0) || ((_arrayResolution.y & (_arrayResolution.y - 1)) != 0)) {
                    EditorGUILayout.HelpBox("Array resolution is not a power of 2 mipmaps may break.\nBe sure to check the array after creation", MessageType.Warning);
                }

                // If resolution is not a square, mipmaps may break
                else if (_arrayResolution.x != _arrayResolution.y) {
                    EditorGUILayout.HelpBox("Array resolution is not a square, mipmaps may break.\nBe sure to check the array after creation", MessageType.Warning);
                }
            }
        }

        /// <summary>
        /// Called when the add button is clicked in the ReorderableList GUI<br />
        /// base.OnTextureAdd() will add a texture to the texture lists and should be called
        /// </summary>
        /// <param name="list">
        /// The ReorderableList used for displaying the GUI
        /// </param>
        protected virtual void OnTextureAdd(ReorderableList list)
        {
            _textures.Add(null);
            _texturesResizing.Add(false);
            _textureErrors.Add(false);
        }

        /// <summary>
        /// Called when the remove button or delete key is pressed in the ReorderableList GUI<br />
        /// base.OnTextureRemove() will remove the selected texture from the texture lists and should be called
        /// </summary>
        /// <param name="list">
        /// The ReorderableList used for displaying the GUI
        /// </param>
        protected virtual void OnTextureRemove(ReorderableList list)
        {
            // If element selected delete that, otherwise delete last element
            int removeIndex = _textures.Count - 1;
            if (_textureROList.selectedIndices.Count > 0)
                removeIndex = _textureROList.selectedIndices[0];

            _textures.RemoveAt(removeIndex);
            _texturesResizing.RemoveAt(removeIndex);
            _textureErrors.RemoveAt(removeIndex);
        }

        /// <summary>
        /// Called when the textures are reordered in the ReorderableList GUI<br />
        /// base.OnTexturesReorder() will handle reordering the texture lists and should be called
        /// </summary>
        /// <param name="list">
        /// The ReorderableList used for displaying the GUI
        /// </param>
        /// <param name="oldActiveElement">
        /// The previous index of the reordered element
        /// </param>
        /// <param name="newActiveElement">
        /// The new index of the reordered element
        /// </param>
        protected virtual void OnTexturesReorder(ReorderableList list, int oldActiveElement, int newActiveElement)
        {
            // Update texture data for reordered indexs, texture is automatically handled

            if (newActiveElement > oldActiveElement)
                newActiveElement--;

            bool resizing = _texturesResizing[oldActiveElement];
            bool error = _textureErrors[oldActiveElement];

            _texturesResizing.RemoveAt(oldActiveElement);
            _textureErrors.RemoveAt(oldActiveElement);

            _texturesResizing.Insert(newActiveElement, resizing);
            _textureErrors.Insert(newActiveElement, error);
        }

        /// <summary>
        /// Called every update while anything is dragged over the window
        /// </summary>
        /// <param name="objectReferences">
        /// The items that are being dragged
        /// </param>
        protected virtual void OnDragUpdate(Object[] objectReferences)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

            if (objectReferences.Any(x => x is Texture2D)) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        /// <summary>
        /// Called when the drag is complete over the window
        /// </summary>
        /// <param name="objectReferences">
        /// The items that are being dragged
        /// </param>
        protected virtual void OnDragPerform(Object[] objectReferences)
        {
            // Return if dragged are null or includes no textures
            if (!objectReferences.Any(x => x != null) && !objectReferences.Any(x => x is Texture2D))
                return;

            // Check if any textures are assigned
            bool anyTexturesAssigned = false;
            if (_textures.Count > 0)
                anyTexturesAssigned = _textures.Any(x => x != null);

            // If all textures are null remove them and replace with dragged
            if (!anyTexturesAssigned) {
                _textures.Clear();
                _texturesResizing.Clear();
                _textureErrors.Clear();

                // Set array resolution to first texture
                Texture2D firstTexture = null;
                for (int i = 0; i < objectReferences.Length; i++) {
                    if (objectReferences[i] is Texture2D) {
                        firstTexture = (Texture2D)objectReferences[i];
                        break;
                    }
                }

                if (firstTexture == null)
                    return;

                _arrayResolution = new Vector2Int(firstTexture.width, firstTexture.height);
            }

            // Insert dragged textures
            for (int i = 0; i < objectReferences.Length; i++) {
                if (objectReferences[i] is not Texture2D || objectReferences[i] == null)
                    continue;

                _textures.Add((Texture2D)objectReferences[i]);
                _texturesResizing.Add(false);
                _textureErrors.Add(false);
            }
        }

        /// <summary>
        /// Checks if any textures exist in the list
        /// </summary>
        /// <returns>
        /// If any textures exist in the list
        /// </returns>
        protected bool ArrayTexturesExist()
        {
            int firstIndexNonNull = _textures.FindIndex(x => x != null);

            // If all textures are null return false
            if (firstIndexNonNull < 0)
                return false;

            return firstIndexNonNull < _textures.Count;
        }

        /// <summary>
        /// Creates the array using the specified textures and settings at a given path
        /// </summary>
        /// <param name="path">
        /// The file location that the array will be created within the project folder<br />
        /// Should always start with "Assets/"
        /// </param>
        /// <returns>
        /// If the operation was successful
        /// </returns>
        protected bool CreateArray(string path)
        {
            // Check if textures exist within the selected array depth
            if (!ArrayTexturesExist())
                return false;

            // Resize textures that need to be resized
            List<Texture2D> arrayTextures = new List<Texture2D>();
            for (int i = 0; i < _textures.Count; i++) {
                if (_textures[i] == null) continue;

                if (_texturesResizing[i] || _resizeTextures)
                    arrayTextures.Add(TextureUtilities.ResizeTexture(_textures[i], _arrayResolution.x, _arrayResolution.y, _resizeFilterMode));
                else
                    arrayTextures.Add(_textures[i]);
            }

            // Create array
            Texture2DArray array = Texture2DArrayUtilities.CreateArrayUserInput(arrayTextures.ToArray(), Texture2DArrayUtilities.SUPPORTED_TEXTURE_FORMATS[_arrayTextureFormatIndex], null, _arrayMipMaps, _arrayLinear);
            if (array == null)
                return false;

            array.wrapMode = _arrayWrapMode;
            array.filterMode = _arrayFilterMode;
            array.anisoLevel = _arrayAnisoLevel;

            AssetDatabase.CreateAsset(array, path);

            // Select and ping the created array
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = array;
            EditorGUIUtility.PingObject(array);

            return true;
        }
    }
}
#endif