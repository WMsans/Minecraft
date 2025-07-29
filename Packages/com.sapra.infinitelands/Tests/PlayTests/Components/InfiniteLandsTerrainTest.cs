using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace sapra.InfiniteLands.Tests
{
    public class InfiniteLandsTerrainTest
    {    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        private static Vector3[] _positions = {
            new Vector3(1010,0,0), 
            new Vector3(0,-424234,3),
            new Vector3(-232222,423,443),
        };
        private static Vector3[] _rotations = {
            new Vector3(0,0,0),
            new Vector3(0,0,45),
            new Vector3(79,0,-45),
            new Vector3(79,1234,-45),
        };

        private static float[] _distances = {
            10,1000,12331231,-1232
        };

        FloatingOrigin CreateOrigin(){
            var origin = new GameObject();
            return origin.AddComponent<FloatingOrigin>();
        }

        [UnityTest]
        public IEnumerator ISpawnComponent()
        {
            var origin = new GameObject();
            origin.AddComponent<InfiniteLandsTerrain>();
            yield return new WaitForEndOfFrame();
            Object.Destroy(origin.gameObject);
        }
    }
}
