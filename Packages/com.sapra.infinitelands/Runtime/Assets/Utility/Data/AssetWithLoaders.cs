using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    public struct AssetWithLoaders<T>
    {
        public IAsset asset;
        public T casted;
        public ILoadAsset[] loaders;

        public bool Equals(AssetWithLoaders<T> casted)
        {
            return asset.Equals(casted.asset) && loaders.Length == casted.loaders.Length;
        }


        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool SequenceEqual(List<AssetWithLoaders<T>> A, List<AssetWithLoaders<T>> B)
        {
            if (A == null || B == null)
                return false;

            if (A.Count != B.Count)
                return false;

            for (int i = 0; i < A.Count; i++)
            {
                var valA = A[i];
                var valB = B[i];

                if (!valA.Equals(valB))
                    return false;
            }

            return true;
        }
    }
}