using UnityEngine;

namespace sapra.InfiniteLands{
    [System.Serializable]
    public class RenderingSettings 
    {
        public float MinDistanceBetweenItems;
        public float MaxDistanceBetweenItems = -1;
        [Min(0)]public float ViewDistance = 1;
    }
}