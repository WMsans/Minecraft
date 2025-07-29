using System;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class FloatingPoint : MonoBehaviour
    {
        private FloatingOrigin origin;
        public bool LockX;
        public bool LockY;
        public bool LockZ;

        private bool withOrigin;

        public Action<Vector3> OnOffsetAdded;

        // Start is called before the first frame update
        void Awake()
        {
            origin = FindAnyObjectByType<FloatingOrigin>();
            withOrigin = origin != null;
        }
        void OnDisable()
        {
            if(withOrigin)
                origin.OnOriginMove -= OnOriginShift;
        }
        void OnEnable()
        {
            if(withOrigin)
                origin.OnOriginMove += OnOriginShift;
        }
        private void OnOriginShift(Vector3Double newOrigin, Vector3Double previousOrigin){
            Vector3 offset = previousOrigin-newOrigin;
            offset.x = LockX ? 0: offset.x;
            offset.y = LockY ? 0: offset.y;
            offset.z = LockZ ? 0: offset.z;

            transform.position += offset;
            OnOffsetAdded?.Invoke(offset);
        }
    }
}
