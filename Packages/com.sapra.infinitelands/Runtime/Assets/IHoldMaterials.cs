using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IHoldMaterials
    {
        public IEnumerable<Material> GetMaterials();

    }
}