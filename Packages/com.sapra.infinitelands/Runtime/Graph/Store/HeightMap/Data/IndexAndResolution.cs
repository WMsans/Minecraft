
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct IndexAndResolution{
        public int Resolution;
        public int Length;
        public int StartIndex;

        public void LogIt()
        {
            Debug.Log(Resolution);
            Debug.Log(Length);
            Debug.Log(StartIndex);
        }

        public IndexAndResolution(int startIndex, int resolution, int length)
        {
            Resolution = resolution;
            Length = length;
            StartIndex = startIndex;
        }

        public static IndexAndResolution CopyAndOffset(IndexAndResolution og, int indexOffset)
        {
            var newData = new IndexAndResolution(og.StartIndex + indexOffset*og.Length, og.Resolution, og.Length);
            return newData;
        }
    }
}