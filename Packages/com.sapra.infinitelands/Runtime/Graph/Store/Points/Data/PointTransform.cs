using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands{
    public struct PointTransform
    {
        public float3 Position;
        public float YRotation;
        public float Scale;

        public PointTransform UpdatePosition(float3 position)
        {
            return new PointTransform()
            {
                Position = position,
                YRotation = YRotation,
                Scale = Scale
            };
        }

        public PointTransform UpdateRotation(float value)
        {
            return new PointTransform()
            {
                Position = Position,
                YRotation = value,
                Scale = Scale
            };
        }

        public PointTransform UpdateScale(float scale)
        {
            return new PointTransform()
            {
                Position = Position,
                YRotation = YRotation,
                Scale = scale
            };
        }

        public void MultiplyMatrix(Matrix4x4 localToWorldMatrix, Vector3 baseScale, Vector3 baseEuler, out Vector3 position, out Quaternion rotation, out Vector3 finalScale)
        {
            position = localToWorldMatrix.MultiplyPoint(Position);
            rotation = Quaternion.Euler(0, YRotation, 0);
            Vector3 worldEuler = (localToWorldMatrix.rotation * rotation).eulerAngles;
            rotation = Quaternion.Euler(worldEuler+baseEuler);

            Vector3 matrixScale = localToWorldMatrix.lossyScale;
            finalScale = new Vector3(
                baseScale.x * matrixScale.x * Scale,
                baseScale.y * matrixScale.y * Scale,
                baseScale.z * matrixScale.z * Scale
            );
        }
    }
}