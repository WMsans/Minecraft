using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    public class SetGlobalLightDirection : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            Shader.SetGlobalVector("_LightDirection", this.transform.forward);
        }
    }
}