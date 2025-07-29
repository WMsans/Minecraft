using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif

namespace sapra.InfiniteLands
{
    [Serializable]
    public class StickyNoteBlock
    {
        public string guid;
        public string title;
        public string content;
        public Vector2 position;
        public Vector2 size;

        #if UNITY_EDITOR
        public StickyNoteTheme theme = StickyNoteTheme.Classic;
        public StickyNoteFontSize fontsize = StickyNoteFontSize.Medium;
        #endif
    }
}