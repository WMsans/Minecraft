using UnityEngine;

namespace sapra.InfiniteLands{
    public static class GraphSettingsController
    {
        private static GraphSettings Instance;
        public static void ChangeValueSettings(float meshScale, Vector2 worldOffset, int seed)
        {
            GraphSettings current = GetSettings();
            current.MeshScale = meshScale;
            current.WorldOffset = worldOffset;
            current.Seed = seed;
        }
        public static GraphSettings GetSettings()
        {
            if (Instance == null)
                Instance = ScriptableObject.CreateInstance<GraphSettings>();
            return Instance;
        }

        private static PortData PreviewPortData;
        public static bool IsPortThePreview(PortData portData)
        {
            if (PreviewPortData.nodeGuid == null)
                return false;
            return PreviewPortData.nodeGuid == portData.nodeGuid && PreviewPortData.fieldName == portData.fieldName && PreviewPortData.listIndex == portData.listIndex;
        }
        public static bool ValidateNode(IGraph graph, out InfiniteLandsNode node)
        {
            node = null;
            if (PreviewPortData.nodeGuid == null || PreviewPortData.nodeGuid == "")
                return false;

            var nodeFound = graph.GetNodeFromGUID(PreviewPortData.nodeGuid);
            if (nodeFound == null)
                return false;

            node = nodeFound;
            return nodeFound.isValid;
        }

        public static bool ValidateNode<T>(IGraph graph, out InfiniteLandsNode node)
        {
            bool basicValid = ValidateNode(graph, out node);
            if (!basicValid)
                return false;
            var fieldType = RuntimeTools.GetTypeFromOutputField(PreviewPortData.fieldName, node, graph);
            return typeof(T).IsAssignableFrom(fieldType);
        }

        public static bool TryGetPreviewData<T>(IGraph graph, BranchData branch, out T resultingData)
        {
            bool isValid = ValidateNode<T>(graph, out var node);
            resultingData = default;

            if (!isValid) return false;

            var writeableNode = branch.GetWriteableNode(node);
            if (!writeableNode.ProcessNode(branch)) return false;

            return writeableNode.TryGetOutputData(branch, out resultingData, PreviewPortData.fieldName, PreviewPortData.listIndex);
        }

        public static void SetPortData(PortData portData)
        {
            PreviewPortData = portData;
        }

        public static void Clear()
        {
            PreviewPortData = default;
        }
    } 
}
