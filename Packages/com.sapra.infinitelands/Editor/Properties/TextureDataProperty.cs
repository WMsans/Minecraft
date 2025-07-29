using UnityEditor;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    
    [CustomPropertyDrawer(typeof(TextureData))]
    public class TextureDataProperty: PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin property
        EditorGUI.BeginProperty(position, label, property);

        // Calculate row heights and spacing
        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Get properties
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty textureProp = property.FindPropertyRelative("texture");
        SerializedProperty defaultColorProp = property.FindPropertyRelative("defaultColor");
        SerializedProperty textureNameProp = property.FindPropertyRelative("textureName");

        bool isDisabled = typeProp.enumValueIndex!=(int)TextureType.Other;

        float size = 100;

        // First row: type and texture
        Rect typeRect = new Rect(position.x, position.y, size, singleLineHeight);
        Rect textureRect = new Rect(position.x + size + spacing, position.y, position.width - size - spacing, singleLineHeight);
        
        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
        EditorGUI.PropertyField(textureRect, textureProp, GUIContent.none);

        float enumSize = 80;
        // Second row: defaultColor and textureName
        Rect defaultColorRect = new Rect(position.x+position.width - enumSize*2f, position.y + singleLineHeight + spacing, enumSize, singleLineHeight);
        Rect defaultColorFieldRect = new Rect(position.x+position.width - enumSize, position.y + singleLineHeight + spacing, enumSize, singleLineHeight);

        float total = defaultColorRect.width+defaultColorFieldRect.width+spacing;

        Rect textureNameRect = new Rect(position.x , position.y + singleLineHeight + spacing, size - spacing, singleLineHeight);
        Rect textureFieldRect = new Rect(position.x +size+spacing, position.y + singleLineHeight + spacing, position.width-total-size-spacing, singleLineHeight);

        string targetName = TextureDataExtensions.DefaultNameFromType((TextureType)typeProp.enumValueIndex, textureNameProp.stringValue);
        TextureDefault defaultTexture = isDisabled ? TextureDataExtensions.DefaultTextureNameFromType((TextureType)typeProp.enumValueIndex) : (TextureDefault)defaultColorProp.enumValueIndex;
        EditorGUI.BeginDisabledGroup(isDisabled);
        EditorGUI.LabelField(defaultColorRect, new GUIContent(defaultColorProp.displayName));
        defaultColorProp.enumValueIndex = (int)(TextureDefault)EditorGUI.EnumPopup(defaultColorFieldRect, defaultTexture);

        EditorGUI.LabelField(textureNameRect, new GUIContent(textureNameProp.displayName));
        textureNameProp.stringValue = EditorGUI.TextField(textureFieldRect,targetName);
        EditorGUI.EndDisabledGroup();

        // End property
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Two rows of fields, with spacing between them
        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return (singleLineHeight * 2) + spacing;
    }
    }
}