using UnityEngine;

namespace sapra.InfiniteLands
{
    public class BoundedCurveAttribute : PropertyAttribute
    {
        public readonly Rect bounds;

        public BoundedCurveAttribute(float xMin, float yMin, float xMax, float yMax)
        {
            this.bounds = new Rect(xMin, yMin, xMax, yMax);
        }

        public BoundedCurveAttribute(float xMax, float yMax)
        {
            this.bounds = new Rect(0, 0, xMax, yMax);
        }

        public BoundedCurveAttribute()
        {
            this.bounds = new Rect(0, 0, 1, 1);
        }

    }
}