// https://docs.unity3d.com/Manual/30_search.html?q=ComputeBuffer
// https://docs.unity3d.com/ScriptReference/ComputeBuffer.html

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;

        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = dir.x;
            direction.y = dir.y;
            direction.z = dir.z;
            noise_offset = offset;
        }
    }

    public ComputeShader shader;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public Mesh boidMesh; // Unity creates mesh assets when it imports model,
                          // but you can also create mesh assets directly in Unity;
                          // for example, by creating a mesh with code
    public Material boidMaterial; // You use meshes to describe shapes,
                                  // and materials to describe the appearance of surfaces.
    public int boidsCount;
    public float spawnRadius;
    public Transform target; // A GameObject will always have a Transform component attached
                             // - it is not possible to remove a Transform or to create a GameObject without one.

    int kernelHandle;
    ComputeBuffer boidsBuffer;  // GPU data buffer, mostly for use with compute shaders.
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 }; // Instancing args

    Boid[] boidsArray;
    int groupSizeX;
    int numOfBoids;
    Bounds bounds; // Represents an axis aligned bounding box, fully enclosing some object.

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;

        // Get thread count of block
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        // bounds with a center of (0,0,0) and a size of (1000,1000,1000)
        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        InitBoids();
        InitShader();
    }

    private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            // This code is attached to a unity game object in the GUI editor named "Flock"
            // transform.position gets the position of that game object.
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            float offset = Random.value * 1000.0f;
            boidsArray[i] = new Boid(pos, rot.eulerAngles, offset);
        }
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 7 * sizeof(float)); // Allocate GPU data
        boidsBuffer.SetData(boidsArray); // Copy data to the GPU

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments); // Allocate GPU data
        if (boidMesh != null)
        {
            args[0] = (uint)boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args); // Copy data to the GPU

        // Set data for the compute shader
        shader.SetBuffer(this.kernelHandle, "boidsBuffer", boidsBuffer);
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", numOfBoids);

        // Set data for the render shader
        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(this.kernelHandle, groupSizeX, 1, 1);

        // Similar to Graphics.DrawMeshInstanced, this function draws many instances of the same mesh,
        // but unlike that method, the arguments for how many instances to draw come from bufferWithArgs.

        //     mesh: 
        //     submeshIndex: Which subset of the mesh to draw. This applies only to meshes that are composed of several materials.
        //     material: 
        //     bounds: The bounding volume surrounding the instances you intend to draw.
        //     bufferWithArgs:
        //     argsOffset:
        //     properties:
        //     castShadows: = ShadowCastingMode.On
        //     receiveShadows: = true
        //     layer: Layers are a tool that allows you to separate to separate GameObjects in your scenes.
        //            You can use layers through the UI and with scripts to edit how GameObjects within your scene interact with each other.
        //     camera:
        //     lightProbeUsage: light probes store “baked” information about lighting in your scene. store information about light passing through empty space in your scene.
        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer);

        //Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);

        // Graphics.DrawMeshInstanced
        // Similar to Graphics.DrawMesh, this function draws meshes for one frame without the overhead of creating unnecessary game objects.
        // Note: You can only draw a maximum of 1023 instances at once.
    }

    void OnDestroy()
    {
        if (boidsBuffer != null)
        {
            boidsBuffer.Dispose();
        }

        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
        }
    }
}

