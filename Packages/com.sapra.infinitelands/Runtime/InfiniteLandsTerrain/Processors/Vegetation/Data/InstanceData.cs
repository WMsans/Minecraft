using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct InstanceData{
        private Vector3 Position;
        private uint2 QuaternionAndScale;
        private uint NormalAndTexture;
        public static int size => sizeof(float)*3 + sizeof(uint)*2+ sizeof(uint);
        
        public void PerformShift(Vector3 offset) => Position += offset;
        public Vector3 GetPosition() => Position;
        public uint GetTextureIndex() => (NormalAndTexture >> 24) & 0x7F;
        public bool GetValidity() => (NormalAndTexture & 0x80000000) != 0;

        public Quaternion GetRotation()
        {
            uint qx = QuaternionAndScale.x & 0xFFFF;
            uint qy = (QuaternionAndScale.x >> 16) & 0xFFFF;
            uint qz = QuaternionAndScale.y & 0xFFFF;
            float3 qxyz = (new float3(qx,qy,qz)/0xFFFF)*2.0f-1.0f;
            float qw = 1-(qxyz.x*qxyz.x+qxyz.y*qxyz.y+qxyz.z*qxyz.z);
            return new Quaternion(qxyz.x, qxyz.y, qxyz.z, qw > 0.0f ? math.sqrt(qw):0.0f);
        }
        public Vector3 GetScale()
        {
            float s = math.f16tof32((QuaternionAndScale.y >> 16) & 0xFFFF);
            return new Vector3(s,s,s);
        }
        public Vector3 GetNormalVector()
        {
            uint nx = NormalAndTexture & 0xFFF;
            uint nz = (NormalAndTexture >> 12) & 0xFFF;
            float2 nxz = (new float2(nx,nz)/0xFFF)*2.0f-1.0f;
            float ny = 1.0f-(nxz.x*nxz.x+nxz.y*nxz.y);
            return Vector3.Normalize(new Vector3(nxz.x, ny > 0.0f ? math.sqrt(ny):0.0f, nxz.y));
        }
    }
}