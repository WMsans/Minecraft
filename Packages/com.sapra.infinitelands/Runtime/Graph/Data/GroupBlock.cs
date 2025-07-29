using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace sapra.InfiniteLands
{
    [Serializable]
    public class GroupBlock
    {
        public string guid;
        public string Name;
        public Vector2 position;
        public List<string> elementGuids;
    }
}