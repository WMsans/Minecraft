using System.Text.RegularExpressions;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class AutoOrganizeTerrains : MonoBehaviour
    {
        public void OrganizeTerrains(){
            Terrain[] terrains = GetComponentsInChildren<Terrain>();
            foreach(Terrain terrain in terrains){
                var terrainData = terrain.terrainData;
                Vector3 parse = ParseVector3(terrainData.name);
                terrain.transform.position = new Vector3(parse.x, 0, parse.y)*terrainData.size.x;
            }
        }
        
        public static Vector3 ParseVector3(string vectorString)
        {
            // Pattern to match numbers inside parentheses
            string pattern = @"\((-?\d+\.?\d*),\s*(-?\d+\.?\d*),\s*(-?\d+\.?\d*)\)";
            Match match = Regex.Match(vectorString, pattern);

            if (match.Success)
            {
                // Parse the x, y, and z components
                float x = float.Parse(match.Groups[1].Value);
                float y = float.Parse(match.Groups[2].Value);
                float z = float.Parse(match.Groups[3].Value);

                return new Vector3(x, y, z);
            }
            else
            {
                throw new System.FormatException("Input string is not in the correct format.");
            }
        }
    }
}