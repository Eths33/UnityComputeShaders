using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancingCustomShader : MonoBehaviour
{

    public Material material;

    // Start is called before the first frame update
    void Start()
    {
        //if (!material.IsKeywordEnabled("_TSANIM_BLENDING")) // Checks whether a global shader keyword is enabled.
        //    material.EnableKeyword("_TSANIM_BLENDING");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
