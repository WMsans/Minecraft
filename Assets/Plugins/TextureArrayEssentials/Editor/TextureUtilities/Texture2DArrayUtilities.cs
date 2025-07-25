using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

namespace TextureArrayEssentials
{
    using CustomDialog;

    public static class Texture2DArrayUtilities
    {
        // Supported texture formats
        public static readonly TextureFormat[] SUPPORTED_TEXTURE_FORMATS = {
            // Popular formats first, rest in order
            TextureFormat.DXT1,
            TextureFormat.DXT5,
            TextureFormat.RGBA32,
            TextureFormat.ARGB32,

            TextureFormat.Alpha8,
            TextureFormat.ARGB4444,
            TextureFormat.RGB24,
            TextureFormat.RGB565,
            TextureFormat.R16,
            TextureFormat.RGBA4444,
            TextureFormat.BGRA32,
            TextureFormat.RHalf,
            TextureFormat.RGHalf,
            TextureFormat.RFloat,
            TextureFormat.RGFloat,
            TextureFormat.RGBAFloat,
            TextureFormat.RGB9e5Float,
            TextureFormat.BC4,
            TextureFormat.BC5,
            TextureFormat.BC6H,
            TextureFormat.BC7,
            TextureFormat.RG16,
            TextureFormat.R8,
            TextureFormat.RG32,
            TextureFormat.RGB48,
            TextureFormat.RGBA64
        };

        /// <summary>
        /// Creates a new Texture2DArray with the given textures taking into account unsupported formats<br />
        /// Resolution will be the resolution of the first input texture<br />
        /// Texture will be skipped if it is not the same resolution of the first
        /// </summary>
        /// <param name="textures">
        /// The textures that will be in the array
        /// </param>
        /// <param name="textureFormat">
        /// The texture format of the output array
        /// </param>
        /// <param name="transferMipmaps">
        /// If mipmaps will be transferred from the textures to the array
        /// </param>
        /// <param name="linear">
        /// If the output array will be linear<br />
        /// Recommended in the Built-In Render Pipeline only when including normal maps<br />
        /// Not Recommended in URP/HDRP as it will result in brighter textures
        /// </param>
        /// <returns>
        /// A Texture2DArray with the input textures and settings
        /// </returns>
        public static Texture2DArray CreateArray(Texture2D[] textures, TextureFormat textureFormat, bool transferMipmaps = true, bool linear = false)
        {
            // Check if configuration is supported
            if (!SystemInfo.supports2DArrayTextures) {
                Debug.LogError("Cannot create a Texture2DArray as they are unsupported on this system");
                return null;
            }

            if (!SystemInfo.SupportsTextureFormat(textureFormat)) {
                Debug.LogError($"Cannot create array, the format ({textureFormat}) is unsupported on this system");
                return null;
            }

            if (!SUPPORTED_TEXTURE_FORMATS.Contains(textureFormat)) {
                Debug.LogError($"Cannot create array, the format ({textureFormat}) is unsupported");
                return null;
            }

            // Initial resolution
            Vector2Int resolution = new Vector2Int(textures[0].width, textures[0].height);

            // Create array and assign textures to it
            Texture2DArray array = new Texture2DArray(resolution.x, resolution.y, textures.Length, textureFormat, transferMipmaps ? textures[0].mipmapCount > 1 : false, linear);
            for (int i = 0; i < textures.Length; i++) {
                if (textures[i] == null) continue;

                Texture2D texture = textures[i];

                // If the texture resolution is not the same as the array, skip it
                if (texture.width != resolution.x || texture.height != resolution.y)
                    continue;

                // Add texture to the array
                UpdateTexture(array, texture, i, transferMipmaps);
            }

            array.filterMode = textures[0].filterMode;
            array.wrapMode = textures[0].wrapMode;

            return array;
        }

        /// <summary>
        /// Creates a new Texture2DArray with the given textures taking into account unsupported formats<br />
        /// Resolution will be the resolution of the first input texture<br />
        /// Automatically resizes all textures to the resolution of the first
        /// </summary>
        /// <param name="textures">
        /// The textures that will be in the array
        /// </param>
        /// <param name="textureFormat">
        /// The texture format of the output array
        /// </param>
        /// <param name="transferMipmaps">
        /// If mipmaps will be transferred from the textures to the array
        /// </param>
        /// <param name="linear">
        /// If the output array will be linear<br />
        /// Recommended in the Built-In Render Pipeline only when including normal maps<br />
        /// Not Recommended in URP/HDRP as it will result in brighter textures
        /// </param>
        /// <returns>
        /// A Texture2DArray with the input textures and settings
        /// </returns>
        public static Texture2DArray CreateArrayAutoResize(Texture2D[] textures, TextureFormat textureFormat, bool transferMipmaps = true, bool linear = false)
        {
            // Initial resolution
            Vector2Int resolution = new Vector2Int(textures[0].width, textures[0].height);

            // Set textures to readable if not already
            TextureUtilities.SetReadable(textures);

            // Check if any texture is a different resolution and resize them if necessary
            for (int i = 0; i < textures.Length; i++) {
                if (textures[i] == null) continue;

                Texture2D texture = textures[i];

                // Resize texture if it is not the same resolution as the array
                if (texture.width != resolution.x || texture.height != resolution.y)
                    textures[i] = TextureUtilities.ResizeTexture(texture, resolution.x, resolution.y);
            }

            // Create array
            return CreateArray(textures, textureFormat, transferMipmaps, linear);
        }

        /// <summary>
        /// Creates a new Texture2DArray with the given textures taking into account unsupported formats<br />
        /// Resolution will be the resolution of the first input texture<br />
        /// Checks for user input to decide whether to resize the texture or array if a texture is at a different resolution to the array
        /// </summary>
        /// <param name="textures">
        /// The textures that will be in the array
        /// </param>
        /// <param name="textureFormat">
        /// The texture format of the output array
        /// </param>
        /// <param name="autoResizeIndexes">
        /// Automatically resizes all textures at these indexes if required instead of showing a prompt to the user<br />
        /// Ex.If this is { 0, 2 }, the textures at index 0 & 2 will be automatically resized
        /// </param>
        /// <param name="transferMipmaps">
        /// If mipmaps will be transferred from the textures to the array
        /// </param>
        /// <param name="linear">
        /// If the output array will be linear<br />
        /// Recommended in the Built-In Render Pipeline only when including normal maps<br />
        /// Not Recommended in URP/HDRP as it will result in brighter textures
        /// </param>
        /// <returns>
        /// A Texture2DArray with the input textures and settings
        /// </returns>
        public static Texture2DArray CreateArrayUserInput(Texture2D[] textures, TextureFormat textureFormat, int[] autoResizeIndexes = null, bool transferMipmaps = true, bool linear = false)
        {
            // Initial resolution
            Vector2Int resolution = new Vector2Int(textures[0].width, textures[0].height);

            // Set textures to readable if not already
            TextureUtilities.SetReadable(textures);

            // Check if any texture is a different resolution
            for (int i = 0; i < textures.Length; i++) {
                if (textures[i] == null) continue;

                Texture2D texture = textures[i];

                if (texture.width != resolution.x || texture.height != resolution.y) {
                    // Automatically resize and skip popup if specified
                    if (autoResizeIndexes.Contains(i)) {
                        textures[i] = TextureUtilities.ResizeTexture(texture, resolution.x, resolution.y);
                        continue;
                    }

                    // Get user input to determine what to do with the texture
                    int returned = ShaderGUIDialog.DisplayDialogComplex(
                        "Texture Resolution Difference",
                        $"Texture: {texture.width}x{texture.height} Array: {resolution.x}x{resolution.y}\n"
                        + "Texture size is not the same as they array. Would you like to resize this texture to the array resolution, or resize the array to this texture resolution?",
                        "Resize Texture", "Cancel", "Resize Array"
                    );

                    // If resizing texture, resize it and assign back to the texture array
                    if (returned == 0)
                        textures[i] = TextureUtilities.ResizeTexture(texture, resolution.x, resolution.y);

                    // If cancelling, skip texture in the array
                    if (returned == 1) {
                        textures[i] = null;
                    }

                    // If resizing array, skip the next textures and move onto resizing all of them
                    if (returned == 2) {
                        resolution = new Vector2Int(texture.width, texture.height);
                        break;
                    }
                }
            }

            // If resolution has changed, resize all textures to the new resolution
            if(resolution != new Vector2Int(textures[0].width, textures[0].height)) {
                for (int i = 0; i < textures.Length; i++) {
                    if (textures[i] == null) continue;

                    Texture2D texture = textures[i];
                    if (texture.width != resolution.x || texture.height != resolution.y) {
                        textures[i] = TextureUtilities.ResizeTexture(texture, resolution.x, resolution.y);
                    }
                }
            }

            // Create array
            return CreateArray(textures, textureFormat, transferMipmaps, linear);
        }

        /// <summary>
        /// Overwrites the texture in the input array at a given index to the input texture<br />
        /// Uses GPU functions to speed up operations<br />
        /// Update will not be applied if the texture resolution is not the same as the array
        /// </summary>
        /// <param name="array">
        /// The array that will be updated
        /// </param>
        /// <param name="texture">
        /// The texture that will be inserted into the array
        /// </param>
        /// <param name="index">
        /// The index which the texture will overwrite
        /// </param>
        /// <param name="transferMipmaps">
        /// If mipmaps will be transferred from the texture to the array if possible
        /// </param>
        /// <returns>
        /// The modified array with the texture inserted
        /// </returns>
        public static Texture2DArray UpdateTexture(Texture2DArray array, Texture2D texture, int index, bool transferMipmaps = true)
        {
            // Set texture to readable if not already
            TextureUtilities.SetReadable(texture);

            // Do not update if texture does not have the same resolution of the array
            if (texture.width != array.width || texture.height != array.height)
                return array;

            // Change texture format if not the same as array
            if (texture.format != array.format) {
                // Convert to RGBA32 first, supports more formats
                Texture2D temp = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, transferMipmaps ? texture.mipmapCount > 1 : false);
                Color[] pixels = texture.GetPixels();
                temp.SetPixels(pixels);
                temp.Apply();

                // Compress texture to array format if array is compressed
                if (TextureUtilities.COMPRESSED_TEXTURE_FORMATS.Contains(array.format))
                    EditorUtility.CompressTexture(temp, array.format, TextureCompressionQuality.Normal);

                texture = temp;
            }

            // Copy texture to array
            for (int mip = 0; mip < (transferMipmaps ? texture.mipmapCount : 1); mip++) {
                Graphics.CopyTexture(texture, 0, mip, array, index, mip);
            }

            return array;
        }

        /// <summary>
        /// Overwrites the texture in the input array at a given index to the input texture<br />
        /// Automatically resizes the input texture to the array size if it is a different resolution
        /// </summary>
        /// <param name="array">
        /// The array that will be updated
        /// </param>
        /// <param name="texture">
        /// The texture that will be inserted into the array
        /// </param>
        /// <param name="index">
        /// The index which the texture will overwrite
        /// </param>
        /// <param name="transferMipmaps">
        /// If mipmaps will be transferred from the texture to the array if possible
        /// </param>
        /// <returns>
        /// The modified array with the texture inserted
        /// </returns>
        public static Texture2DArray UpdateTextureAutoResize(Texture2DArray array, Texture2D texture, int index, bool transferMipmaps = true)
        {
            // If array has a depth of one, the only texture is being replaced, create a new array as it will cause mipmap or format errors otherwise
            if (array.depth == 1) {
                array = CreateArrayUserInput(new Texture2D[] { texture }, array.format, null, array.mipmapCount > 1);
                return array;
            }

            // Set texture to readable if not already
            TextureUtilities.SetReadable(texture);

            // Resize texture if resolution is different to the array
            if (texture.width != array.width || texture.height != array.height) {
                Debug.LogWarning($"Texture at index {index} is not the same resolution as the array, resizing to array size. Please use a texture with the same resolution as the array");
                // Scale texture to array resolution
                texture = TextureUtilities.ResizeTexture(texture, array.width, array.height);
            }

            UpdateTexture(array, texture, index, transferMipmaps);

            return array;
        }

        /// <summary>
        /// Overwrites the texture in the input array at a given index to the input texture<br />
        /// Waits for the user to input the outcome when a texture is a different resolution to the array
        /// </summary>
        /// <param name="array">
        /// The array that will be updated
        /// </param>
        /// <param name="texture">
        /// The texture that will be inserted into the array
        /// </param>
        /// <param name="index">
        /// The index which the texture will overwrite
        /// </param>
        /// <param name="transferMipmaps">
        /// If mipmaps will be transferred from the texture to the array if possible
        /// </param>
        /// <returns>
        /// Item1: Modified Array, Item2: User Cancelled
        /// </returns>
        public static (Texture2DArray, bool) UpdateTextureUserInput(Texture2DArray array, Texture2D texture, int index, bool transferMipmaps = true)
        {
            // If array has a depth of one, the only texture is being replaced, create a new array as it will cause mipmap or format errors otherwise
            if(array.depth == 1) {
                array = CreateArrayUserInput(new Texture2D[] { texture }, array.format, null, array.mipmapCount > 1);
                return (array, false);
            }

            // Only give user option if texture differs from its resolution
            if(texture.width != array.width || texture.height != array.height) {
                // Get user input to determine what to do with the texture
                int returned = ShaderGUIDialog.DisplayDialogComplex(
                    "Texture Resolution Difference",
                    $"Texture: {texture.width}x{texture.height} Array: {array.width}x{array.height}\n"
                    + "Texture size is not the same as they array. Would you like to resize this texture to the array resolution, or resize the array to this texture resolution?",
                    "Resize Texture", "Cancel", "Resize Array"
                );

                switch (returned) {
                    case 0:
                        // Scale texture to array resolution
                        texture = TextureUtilities.ResizeTexture(texture, array.width, array.height);
                        break;
                    case 1:
                        // Cancel
                        return (array, true);
                    case 2:
                        // Scale array to texture resolution
                        array = ResizeArrayTextures(array, texture.width, texture.height);
                        UpdateTexture(array, texture, index, transferMipmaps);

                        // Return resized array
                        return (array, false);
                }
            }

            UpdateTexture(array, texture, index, transferMipmaps);

            return (array, false);
        }

        /// <summary>
        /// Gets the texture in an input array at a given index
        /// </summary>
        /// <param name="array">
        /// The array that will be searched
        /// </param>
        /// <param name="index">
        /// The index for which texture will be returned
        /// </param>
        /// <returns>
        /// The texture in the array at the specified index
        /// </returns>
        public static Texture2D GetTexture(Texture2DArray array, int index)
        {
            Texture2D texture = new Texture2D(array.width, array.height, array.format, array.mipmapCount > 1);

            // Copy texture from the array
            for (int mip = 0; mip < array.mipmapCount; mip++) {
                Graphics.CopyTexture(array, index, mip, texture, 0, mip);
            }

            return texture;
        }

        /// <summary>
        /// Retrieves all the textures in an input array
        /// </summary>
        /// <param name="array">
        /// The array that will be searched
        /// </param>
        /// <returns>
        /// An array with all the textures from the given array
        /// </returns>
        public static Texture2D[] GetTextures(Texture2DArray array)
        {
            if (array == null)
                return null;

            Texture2D[] textures = new Texture2D[array.depth];

            for (int i = 0; i < array.depth; i++) {
                textures[i] = GetTexture(array, i);
            }

            return textures;
        }

        /// <summary>
        /// Scales all textures in the array to the new specified resolution<br />
        /// Skips textures already at the new resolution
        /// </summary>
        /// <param name="array">
        /// The array that will be searched
        /// </param>
        /// <param name="newWidth">
        /// The width of the output textures
        /// </param>
        /// <param name="newHeight">
        /// The height of the output textures
        /// </param>
        /// <param name="resizeFilterMode">
        /// The filter mode used when resizing the textures
        /// </param>
        /// <returns>
        /// The modified array with all the textures at the new resolution
        /// </returns>
        public static Texture2DArray ResizeArrayTextures(Texture2DArray array, int newWidth, int newHeight, FilterMode resizeFilterMode = FilterMode.Bilinear)
        {
            Texture2D[] textures = GetTextures(array);
            for (int i = 0; i < textures.Length; i++) {
                // Only resize texture if resolution is not the target
                if (textures[i].width != newWidth && textures[i].height != newHeight)
                    textures[i] = TextureUtilities.ResizeTexture(textures[i], newWidth, newHeight, resizeFilterMode);
            }

            return CreateArrayUserInput(textures, array.format);
        }
    }
}
#endif