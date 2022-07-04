using UnityEngine;
using System.Collections;

public class ComputeParticlesDirect_Builtin : MonoBehaviour 
{
	struct Particle
	{
		public Vector3 position;
	};

	public int warpCount = 5;
	public Material material;
	public ComputeShader computeShader;

	private const int warpSize = 32;
	private ComputeBuffer particleBuffer;
	private int particleCount;
	private Particle[] plists;

	void Start () 
	{
		particleCount = warpCount * warpSize;
		
		// Init particles
		plists = new Particle[particleCount];
		for (int i = 0; i < particleCount; ++i)
		{
            plists[i].position = Random.insideUnitSphere * 4f;
        }
		
		//Set data to buffer
		particleBuffer = new ComputeBuffer(particleCount, 12); // 12 = sizeof(Particle)
		particleBuffer.SetData(plists);
		
		//Set buffer to computeShader and Material
		computeShader.SetBuffer(0, "particleBuffer", particleBuffer);
		material.SetBuffer ("particleBuffer", particleBuffer);
	}

	void Update () 
	{
		computeShader.Dispatch(0, warpCount, 1, 1);
	}
    //OnRenderObject is called after camera has rendered the Scene.
    void OnRenderObject()
	{
        //This is mostly used in direct drawing code. For example, drawing 3D primitives with GL.Begin, GL.End, and also drawing meshes using Graphics.DrawMeshNow.
        //���w�ϥ�mat��pass�h��V�o�h
        material.SetPass(0);
        //DrawProceduralNow �b GPU �W����ø�s�եΡA�S�������I�ί��޽w�İϡC
        Graphics.DrawProceduralNow(MeshTopology.Points,1,particleCount);
	}

	void OnDestroy()
	{
		particleBuffer.Release();
	}
}
