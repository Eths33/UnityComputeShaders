using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

public class ParticleFun : MonoBehaviour
{

    private Vector2 cursorPos;

    // struct
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    const int SIZE_PARTICLE = 7 * sizeof(float);

    public int particleCount = 1000000;
    public Material material; // * Handle to the vertex-fragment shader ? *
                              // This class exposes all properties from a material,
                              // allowing you to animate them. You can also use it
                              // to set custom shader properties that can't be accessed
                              // through the inspector (e.g. matrices).

    public ComputeShader shader; // Compute Shader asset.
    
    [Range(1, 10)]
    public int pointSize = 2;

    int kernelID;
    ComputeBuffer dev_particleBuffer;

    int groupSizeX; 
    
    
    // Use this for initialization
    void Start()
    {
        Init();
    }

    void Init()
    {
        // initialize the particles
        Particle[] particleArray = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 v = new Vector3();
            v.x = Random.value * 2f - 1.0f;
            v.y = Random.value * 2f - 1.0f;
            v.z = Random.value * 2f - 1.0f;
            v.Normalize();
            v *= Random.value * 0.5f;

            particleArray[i].position.x = v.x;
            particleArray[i].position.y = v.y;
            particleArray[i].position.z = v.z + 3.0f;

            particleArray[i].velocity = new Vector3(0.0f, 0.0f, 0.0f);

            particleArray[i].life = Random.value * 0.5f + 1.0f;
        }

        // create compute buffer
        dev_particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);

        dev_particleBuffer.SetData(particleArray);

        // find the id of the kernel
        kernelID = shader.FindKernel("CSParticle");

        uint threadsX;
        shader.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadsX);

        // bind the compute buffer to the shader and the compute shader
        // Buffer is shared between compute shader and Vertex-fragment shader
        shader.SetBuffer(kernelID, "particleBuffer", dev_particleBuffer);
        material.SetBuffer("particleBuffer", dev_particleBuffer);

        material.SetInt("_PointSize", pointSize);
    }

    void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
    }

    void OnDestroy()
    {
        if (dev_particleBuffer != null)
            dev_particleBuffer.Release();
    }

    // Update is called once per frame
    void Update()
    {

        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        // Send datas to the compute shader
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetFloats("mousePosition", mousePosition2D);

        // Update the Particles
        shader.Dispatch(kernelID, groupSizeX, 1, 1);
    }

    void OnGUI()
    {
        // https://gamedevbeginner.com/how-to-convert-the-mouse-position-to-world-space-in-unity-2d-3d/
        Vector3 p = new Vector3();
        Camera c = Camera.main;
        Event e = Event.current;
        Vector2 mousePos = new Vector2();

        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        mousePos.x = e.mousePosition.x;
        mousePos.y = c.pixelHeight - e.mousePosition.y;

        p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane + 14));// z = 3.

        cursorPos.x = p.x;
        cursorPos.y = p.y;
        
    }
}
