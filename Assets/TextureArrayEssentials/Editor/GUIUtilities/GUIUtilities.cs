using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace TextureArrayEssentials.GUIUtilities
{
    public static class GUIUtilities
    {
        // Constants
        public const int LINE_HEIGHT = 18;
        public const int LINE_SPACING = 2;

        /// <summary>
        /// Gets the rect of a single line, updating the GUILayout accordingly
        /// </summary>
        /// <param name="heightOverride">
        /// Sets the height of the output rect, defaults to line height
        /// </param>
        /// <returns>
        /// The rect of a single line
        /// </returns>
        public static Rect GetLineRect(float heightOverride = LINE_HEIGHT + LINE_SPACING)
        {
            try {
                return EditorGUILayout.GetControlRect(true, heightOverride, EditorStyles.layerMaskField);
            } catch(Exception e) {
                if (e is NullReferenceException)
                    return new Rect(); // NullReferenceException happens sometimes not sure why but handle it here

                throw e;
            }
        }

        /// <summary>
        /// Draws a Texture2DArray field
        /// </summary>
        /// <param name="array">
        /// The input Texture2DArray
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The Texture2DArray input into the inspector
        /// </returns>
        public static Texture2DArray DrawTexture2DArray(Texture2DArray array, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawTexture2DArray(lineRect, array, content);
        }

        /// <summary>
        /// Draws a Texture2DArray field
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="array">
        /// The input Texture2DArray
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The Texture2DArray input into the inspector
        /// </returns>
        public static Texture2DArray DrawTexture2DArray(Rect rect, Texture2DArray array, GUIContent content)
        {
            if (rect == Rect.zero)
                return array;

            // Get Rects
            Rect thumbRect = EditorGUI.IndentedRect(rect);
            thumbRect.y -= 0f;
            thumbRect.height = 18f;
            thumbRect.width = 32f;
            float num = thumbRect.x + 30f;
            Rect labelRect = new Rect(num, rect.y, thumbRect.x + EditorGUIUtility.labelWidth - num, rect.height);

            EditorGUI.HandlePrefixLabel(rect, labelRect, content, 0, EditorStyles.label);

            return (Texture2DArray)EditorGUI.ObjectField(thumbRect, array, typeof(Texture2DArray), false);
        }

        /// <summary>
        /// Draws a single texture field
        /// </summary>
        /// <param name="texture">
        /// The input texture
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The texture input into the inspector
        /// </returns>
        public static Texture2D DrawTexture(Texture2D texture, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawTexture(lineRect, texture, content);
        }

        /// <summary>
        /// Draws a single texture field
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="texture">
        /// The input texture
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The texture input into the inspector
        /// </returns>
        public static Texture2D DrawTexture(Rect rect, Texture2D texture, GUIContent content)
        {
            if (rect == Rect.zero)
                return texture;

            // Get Rects
            Rect thumbRect = EditorGUI.IndentedRect(rect);
            thumbRect.y -= 0f;
            thumbRect.height = 18f;
            thumbRect.width = 32f;
            float num = thumbRect.x + 30f;
            Rect labelRect = new Rect(num, rect.y, thumbRect.x + EditorGUIUtility.labelWidth - num, rect.height);

            EditorGUI.HandlePrefixLabel(rect, labelRect, content, 0, EditorStyles.label);

            try {
                return (Texture2D)EditorGUI.ObjectField(thumbRect, texture, typeof(Texture2D), false);
            } catch (Exception e) {
                if (e is UnityEngine.ExitGUIException)
                    return texture; // Exit GUI Exception happens when opening select texture popup not sure why but handle it here

                throw e;
            }
        }

        /// <summary>
        /// Draws a single texture field with an accompanied integer value
        /// </summary>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, int) DrawTextureWithInt(Texture2D texture, int intValue, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawTextureWithInt(lineRect, texture, intValue, content);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied integer value
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, int) DrawTextureWithInt(Rect rect, Texture2D texture, int intValue, GUIContent content)
        {
            if (rect == Rect.zero)
                return (texture, intValue);

            // Texture
            texture = DrawTexture(rect, texture, content);

            // Slider
            Rect valueRect = MaterialEditor.GetRectAfterLabelWidth(rect);
            intValue = EditorGUI.IntField(valueRect, intValue);

            return (texture, intValue);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied integer slider
        /// </summary>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, int) DrawTextureWithIntSlider(Texture2D texture, int sliderValue, int sliderMin, int sliderMax, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawTextureWithIntSlider(lineRect, texture, sliderValue, sliderMin, sliderMax, content);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied integer slider
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, int) DrawTextureWithIntSlider(Rect rect, Texture2D texture, int sliderValue, int sliderMin, int sliderMax, GUIContent content)
        {
            if (rect == Rect.zero)
                return (texture, sliderValue);

            // Texture
            texture = DrawTexture(rect, texture, content);

            // Slider
            Rect valueRect = MaterialEditor.GetRectAfterLabelWidth(rect);
            sliderValue = EditorGUI.IntSlider(valueRect, sliderValue, sliderMin, sliderMax);

            return (texture, sliderValue);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied float value
        /// </summary>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, float) DrawTextureWithFloat(Texture2D texture, float floatValue, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawTextureWithFloat(lineRect, texture, floatValue, content);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied float value
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, float) DrawTextureWithFloat(Rect rect, Texture2D texture, float floatValue, GUIContent content)
        {
            if (rect == Rect.zero)
                return (texture, floatValue);

            // Texture
            texture = DrawTexture(rect, texture, content);

            // Slider
            Rect valueRect = MaterialEditor.GetRectAfterLabelWidth(rect);
            floatValue = EditorGUI.FloatField(valueRect, floatValue);

            return (texture, floatValue);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied slider
        /// </summary>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, float) DrawTextureWithSlider(Texture2D texture, float sliderValue, float sliderMin, float sliderMax, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawTextureWithSlider(lineRect, texture, sliderValue, sliderMin, sliderMax, content);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied slider
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, float) DrawTextureWithSlider(Rect rect, Texture2D texture, float sliderValue, float sliderMin, float sliderMax, GUIContent content)
        {
            if (rect == Rect.zero)
                return (texture, sliderValue);

            // Texture
            texture = DrawTexture(rect, texture, content);

            // Slider
            Rect valueRect = MaterialEditor.GetRectAfterLabelWidth(rect);
            sliderValue = EditorGUI.Slider(valueRect, sliderValue, sliderMin, sliderMax);

            return (texture, sliderValue);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied color field
        /// </summary>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, Color) DrawTextureWithColor(Texture2D texture, Color colorValue, bool hdr, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawTextureWithColor(lineRect, texture, colorValue, hdr, content);
        }

        /// <summary>
        /// Draws a single texture field with an accompanied color field
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="texture">
        /// The input texture
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
        public static (Texture2D, Color) DrawTextureWithColor(Rect rect, Texture2D texture, Color colorValue, bool hdr, GUIContent content)
        {
            if (rect == Rect.zero)
                return (texture, colorValue);

            // Texture
            texture = DrawTexture(rect, texture, content);

            // Color
            try {
                Rect colorRect = MaterialEditor.GetRectAfterLabelWidth(rect);
                colorValue = EditorGUI.ColorField(colorRect, new GUIContent(), colorValue, true, !hdr, hdr);
            } catch (Exception e) {
                if (e is UnityEngine.ExitGUIException)
                    return (texture, colorValue); // Exit GUI Exception happens when opening select color popup not sure why but handle it here

                throw e;
            }

            return (texture, colorValue);
        }

        /// <summary>
        /// Draws multiple texture fields in sequence from the given texture array
        /// </summary>
        /// <param name="textures">
        /// The input textures that will be drawn in order
        /// </param>
        /// <param name="content">
        /// The GUIContent for each field in order
        /// If unassigned each texture will be named "Texture 1", "Texture 2", ...
        /// </param>
        /// <returns>
        /// Array of textures input into the inspector
        /// </returns>
        public static Texture2D[] DrawTextures(Texture2D[] textures, GUIContent[] content = null)
        {
            // Check if valid
            if (textures == null)
                return null;

            Texture2D[] returnedTextures = new Texture2D[textures.Length];

            for (int i = 0; i < textures.Length; i++) {
                // Create content if not assigned
                GUIContent currentContent = new GUIContent($"Texture {i + 1}");
                if (content != null && content[i] != null)
                    currentContent = content[i];

                returnedTextures[i] = DrawTexture(textures[i], content[i]);
            }

            return returnedTextures;
        }

        /// <summary>
        /// Draws a Vector2 field removing the space at the end which EditorGUI.Vector2Field draws
        /// </summary>
        /// <param name="value">
        /// The input Vector2 value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The Vector2 input into the inspector
        /// </returns>
        public static Vector2 DrawVector2Field(Vector2 value, GUIContent content)
        {
            Rect lineRect = GetLineRect();
            return DrawVector2Field(lineRect, value, content);
        }

        /// <summary>
        /// Draws a Vector2 field removing the space at the end which EditorGUI.Vector2Field draws
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="value">
        /// The input Vector2 value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The Vector2 input into the inspector
        /// </returns>
        public static Vector2 DrawVector2Field(Rect rect, Vector2 value, GUIContent content)
        {
            if (rect == Rect.zero)
                return value;

            float vector2FieldPadding = 4f;
            float floatFieldLabelPadding = 2f;

            // Get rect
            Rect fieldsRect = MaterialEditor.GetRectAfterLabelWidth(rect);
            float halfWidth = fieldsRect.width / 2;

            // Get Left Float Rect
            Rect leftRect = fieldsRect;
            leftRect.width = halfWidth - vector2FieldPadding;

            // Get Right Float Rect
            Rect rightRect = fieldsRect;
            rightRect.width = halfWidth - vector2FieldPadding;
            rightRect.x += halfWidth + vector2FieldPadding;

            GUI.Label(rect, content);

            // Set Label Width (Shortens regular label padding)
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(new GUIContent("X")).x + floatFieldLabelPadding;

            // Draw Float Fields
            float xVal = EditorGUI.FloatField(leftRect, new GUIContent("X"), value.x);
            float yVal = EditorGUI.FloatField(rightRect, new GUIContent("Y"), value.y);

            // Reset Label Width
            EditorGUIUtility.labelWidth = labelWidth;

            return new Vector2(xVal, yVal);
        }

        /// <summary>
        /// Draws a Vector2Int field removing the space at the end which EditorGUI.Vector2IntField draws
        /// </summary>
        /// <param name="value">
        /// The input Vector2Int value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The Vector2Int input into the inspector
        /// </returns>
        public static Vector2Int DrawVector2IntField(Vector2Int value, GUIContent content)
        {
            Rect lineRect = GetLineRect();

            return DrawVector2IntField(lineRect, value, content);
        }

        /// <summary>
        /// Draws a Vector2Int field removing the space at the end which EditorGUI.Vector2IntField draws
        /// </summary>
        /// <param name="rect">
        /// The space that the field will use
        /// </param>
        /// <param name="value">
        /// The input Vector2Int value
        /// </param>
        /// <param name="content">
        /// The GUIContent for the field
        /// </param>
        /// <returns>
        /// The Vector2Int input into the inspector
        /// </returns>
        public static Vector2Int DrawVector2IntField(Rect rect, Vector2Int value, GUIContent content)
        {
            float vector2FieldPadding = 4f;
            float floatFieldLabelPadding = 2f;

            // Get rect
            Rect fieldsRect = MaterialEditor.GetRectAfterLabelWidth(rect);
            float halfWidth = fieldsRect.width / 2;

            // Get Left Float Rect
            Rect leftRect = fieldsRect;
            leftRect.width = halfWidth - vector2FieldPadding;

            // Get Right Float Rect
            Rect rightRect = fieldsRect;
            rightRect.width = halfWidth - vector2FieldPadding;
            rightRect.x += halfWidth + vector2FieldPadding;

            GUI.Label(rect, content);

            // Set Label Width (Shortens regular label padding)
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(new GUIContent("X")).x + floatFieldLabelPadding;

            // Draw Float Fields
            int xVal = EditorGUI.IntField(leftRect, new GUIContent("X"), value.x);
            int yVal = EditorGUI.IntField(rightRect, new GUIContent("Y"), value.y);

            // Reset Label Width
            EditorGUIUtility.labelWidth = labelWidth;

            return new Vector2Int(xVal, yVal);
        }

        /// <summary>
        /// Begins a background in the same style as a HelpBox using GUILayout.BeginVertical
        /// </summary>
        public static void BeginBackgroundVertical()
        {
            GUIStyle backgroundStyle = new GUIStyle("HelpBox");
            backgroundStyle.padding = new RectOffset(5, 5, 5, 5);
        
            GUILayout.BeginVertical(backgroundStyle);
        }

        /// <summary>
        /// Ends the vertical background, same as calling GUILayout.EndVertical
        /// </summary>
        public static void EndBackgroundVertical()
        {
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Begins a background in the same style as a HelpBox using GUILayout.BeginHorizontal
        /// </summary>
        public static void BeginBackgroundHorizontal()
        {
            GUIStyle backgroundStyle = new GUIStyle("HelpBox");
            backgroundStyle.padding = new RectOffset(5, 5, 5, 5);

            GUILayout.BeginHorizontal(backgroundStyle);
        }

        /// <summary>
        /// Ends the horizontal background, same as calling GUILayout.EndHorizontal
        /// </summary>
        public static void EndBackgroundHorizontal()
        {
            GUILayout.EndHorizontal();
        }
    }
}
#endif