using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InputAttribute : PropertyAttribute,  ICanBeRenamed
    {
        public string name_field{get; private set;}

        public InputAttribute(string namefield = "")
        {
            this.name_field = namefield;
        }
    }
}