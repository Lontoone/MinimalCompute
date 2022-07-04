using UnityEngine;
using UnityEngine.Rendering;

public class DrawIndirect : MonoBehaviour
{
	public int maxCount = 10000;
	public Mesh mesh;
	public Material mat;
	private ComputeBuffer cbDrawArgs;
	private ComputeBuffer cbPoints;
	private CommandBuffer cmd;

	void Start()
	{
		Camera cam = Camera.main;
		int m_ColorRTid = Shader.PropertyToID("_CameraScreenTexture");

		//Create resources
		if (cbDrawArgs == null)
		{
			var args = new int[]
			{
				(int)mesh.GetIndexCount(0),
				1,
				(int)mesh.GetIndexStart(0),
				(int)mesh.GetBaseVertex(0),
				0
			};
            // new ComputeBuffer ( 元素總數 , 每個元素的長度 , 模式 )
            // 預設模式 ComputeBufferType 會 map 成 StructuredBuffer<T> and RWStructuredBuffer<T> in HLSL.
            // IndirectArguments用於 Graphics.DrawProceduralIndirect, ComputeShader.DispatchIndirect or Graphics.DrawMeshInstancedIndirect arguments.
            cbDrawArgs = new ComputeBuffer (1, args.Length * 4, ComputeBufferType.IndirectArguments); //each int is 4 bytes
			cbDrawArgs.SetData (args);
		}
		if (cbPoints == null)
		{
			cbPoints = new ComputeBuffer (maxCount, 12, ComputeBufferType.Append); //pointBuffer is 3 floats so 3*4bytes = 12, see shader
			mat.SetBuffer ("pointBuffer", cbPoints); //Bind the buffer wwith material
            //對應shader的 StructuredBuffer<Particle> pointBuffer;

        }

        //The following workflow method comes from Aras's DX11Examples _DrawProceduralIndirect scene
        cmd = new CommandBuffer();
        cmd.name = "Reflective star"; //名字不影響
		//This binds the buffer we want to store the filtered star positions
        //can write into arbitrary locations of some textures and buffers
        //The UAV indexing varies a bit between different platforms. On DX11 the first valid UAV index is the number of active render targets. So the common case of single render target the UAV indexing will start from 1. Platforms using automatically translated HLSL shaders will match this behaviour. However, with hand-written GLSL shaders the indexes will match the bindings. On PS4 the indexing starts always from 1 to match the most common case.
		cmd.SetRandomWriteTarget(1, cbPoints);
		cmd.GetTemporaryRT(m_ColorRTid,cam.pixelWidth,cam.pixelHeight,24);
        //This blit will send the screen texture to shader and do the filtering
        //If the pixel is bright enough we take the pixel position
        //mat材質: 會輸出使用uv紀錄發光強度的pointBufferOutput陣列
        cmd.Blit(BuiltinRenderTextureType.CameraTarget,m_ColorRTid,mat, 0); //將圖blit至新的rt，src可使用Unity內建buffer或自己的rt
		cmd.ClearRandomWriteTargets();
		cmd.ReleaseTemporaryRT(m_ColorRTid); //取得pointBufferOutput資料就可以清除RT
        cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);//，因為blit會將渲染目標設為dest，要再次設回camera
		//Tells actually how many stars we need to draw
		//Copy the filtered star count to cbDrawArgs[1], which is at 4bytes int offset
        //把cbPoints的數量 (紀錄cbPoints append次數)，寫到cbDrawArgs[1]，offset 4是因為跳過cbDrawArgs[0]
		cmd.CopyCounterValue(cbPoints, cbDrawArgs, 4);
        //Draw the stars
        //bufferWithArgs: The GPU buffer containing the arguments for how many instances of this mesh to draw.
        cmd.DrawMeshInstancedIndirect(mesh,0,mat,1,cbDrawArgs,0); //GPU Instancing 一遍mesh
		Camera.main.AddCommandBuffer(CameraEvent.AfterForwardOpaque,cmd);
	}

	private void ReleaseResources ()
	{
		if (cbDrawArgs != null) cbDrawArgs.Release (); cbDrawArgs = null;
		if (cbPoints != null) cbPoints.Release(); cbPoints = null;
	}
	
	void OnDisable ()
	{
		ReleaseResources ();
	}
}