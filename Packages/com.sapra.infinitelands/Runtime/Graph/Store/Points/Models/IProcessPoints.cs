using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IProcessPoints
    {
        public string processorID{get;}

        /// <summary>
        /// Generate the points that are requested from other nodes
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="previousPoints"></param>
        /// <param name="pointSettings"></param>
        /// <returns></returns>
        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoint, PointGenerationSettings pointSettings);

    }
}