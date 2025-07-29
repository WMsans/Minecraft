using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{

    public class StickyNoteView : StickyNote, IRenderSerializableGraph
    {
        public object GetDataToSerialize() => note;
        Vector2 IRenderSerializableGraph.GetPosition() => note.position;
        public string GetGUID() => note.guid;
        
        public StickyNoteBlock note{get; private set;}
        public StickyNoteView(StickyNoteBlock note):base(note.position){
            this.note = note;
            this.viewDataKey = note.guid;
            title = note.title;
            contents = note.content;
            style.width = note.size.x;
            style.height = note.size.y;
            
            theme = note.theme;
            fontSize = note.fontsize;

            RegisterCallback<StickyNoteChangeEvent>(HandleStickyNoteChange);
        }
        
        private void HandleStickyNoteChange(StickyNoteChangeEvent evt)
        {
            note.title = this.title;
            note.content = this.contents;
            note.size = new Vector2(style.width.value.value, style.height.value.value);
            note.theme = theme;
            note.fontsize = fontSize;
        }
        public override bool IsGroupable() => true;
        public override bool IsCopiable() => true;

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if(note == null)
                return;
            note.position.x = newPos.xMin;
            note.position.y = newPos.yMin;
        }

    }
}