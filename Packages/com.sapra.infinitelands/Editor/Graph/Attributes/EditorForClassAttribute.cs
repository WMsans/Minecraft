using UnityEngine;
using System;

namespace sapra.InfiniteLands.Editor{
    [AttributeUsage(AttributeTargets.Class)]
    public class EditorForClass : PropertyAttribute
    {        
        readonly Type type;        
        public Type target => type;

        public EditorForClass(Type portType)
        {
            this.type = portType;
        }
    }
}