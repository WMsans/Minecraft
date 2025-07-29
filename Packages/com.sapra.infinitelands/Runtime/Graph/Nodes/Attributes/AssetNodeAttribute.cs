using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetNodeAttribute : PropertyAttribute
    {
        public Type DefaultNode;
        public AssetNodeAttribute(Type asset)
        {
            DefaultNode = asset;
        }
    }
}