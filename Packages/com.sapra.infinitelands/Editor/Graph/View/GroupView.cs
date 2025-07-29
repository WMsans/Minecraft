using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    public class GroupView : Group, IRenderSerializableGraph
    {
        public GroupBlock group{get; private set;}
        public object GetDataToSerialize() => group;
        Vector2 IRenderSerializableGraph.GetPosition() => group.position;
        public string GetGUID() => group.guid;

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            group.position.x = newPos.xMin;
            group.position.y = newPos.yMin;
        }

        protected override void OnGroupRenamed(string oldName, string newName)
        {
            base.OnGroupRenamed(oldName, newName);
            group.Name = newName;
            autoUpdateGeometry = true;
        }

        public GroupView(GroupBlock group){
            this.viewDataKey = group.guid;
            this.group = group;
            base.SetPosition(new Rect(group.position, Vector2.zero));
            if(!DebugOptions.DebugMode)
                capabilities -= Capabilities.Groupable;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.GroupStyles));

        }
    }
}