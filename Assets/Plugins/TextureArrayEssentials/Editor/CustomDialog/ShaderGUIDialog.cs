using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace TextureArrayEssentials.CustomDialog
{
    // Originally had another custom EditorWindow here which showed more details
    // Due to multiple errors with the "PropertiesGUI() is being called recursively" error it had to be changed back to the regular DisplayDialog
    // Just a wrapper here to remove this warning that happens only in the ShaderGUI OnGUI for whatever reason

    public static class ShaderGUIDialog
    {
        /// <summary>
        /// Displays a modal dialog removing the ShaderGUI specific warning
        /// </summary>
        /// <param name="title">
        /// Title of the window
        /// </param>
        /// <param name="message">
        /// Message displayed in the dialog
        /// </param>
        /// <param name="ok">
        /// Left button underneath the message
        /// </param>
        /// <param name="cancel">
        /// Right button underneath the message
        /// </param>
        /// <returns>
        /// If the ok button was pressed
        /// </returns>
        public static bool DisplayDialog(string title, string message, string ok, string cancel)
        {
            // Disable unity logger for warning "PropertiesGUI() is being called recursively", only happens in ShaderGUI OnGUI for some reason
            bool wasLogging = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;

            // Display Dialog
            bool input = EditorUtility.DisplayDialog(title, message, ok, cancel);

            // Re-enable logger
            Debug.unityLogger.logEnabled = wasLogging;

            return input;
        }

        /// <summary>
        /// Displays a modal dialog with three buttons removing the ShaderGUI specific warning
        /// </summary>
        /// <param name="title">
        /// Title of the window
        /// </param>
        /// <param name="message">
        /// Message displayed in the dialog
        /// </param>
        /// <param name="ok">
        /// Left button underneath the message
        /// </param>
        /// <param name="cancel">
        /// Right button underneath the message
        /// </param>
        /// <param name="alt">
        /// Middle button underneath the  message
        /// </param>
        /// <returns>
        /// 0, 1, 2 for ok, cancel, alt responses respectively
        /// </returns>
        public static int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt)
        {
            // Disable unity logger for warning "PropertiesGUI() is being called recursively", only happens in ShaderGUI OnGUI for some reason
            bool wasLogging = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;

            // Display Dialog
            int input = EditorUtility.DisplayDialogComplex(title, message, ok, cancel, alt);

            // Re-enable logger
            Debug.unityLogger.logEnabled = wasLogging;

            return input;
        }
    }
}
#endif