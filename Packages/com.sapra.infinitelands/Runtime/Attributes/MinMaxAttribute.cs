using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxAttribute : PropertyAttribute
    {
        public readonly float minValue;
        public readonly float maxValue;
        public MinMaxAttribute(float minValue, float maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public MinMaxAttribute(int minValue, int maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
    }
}