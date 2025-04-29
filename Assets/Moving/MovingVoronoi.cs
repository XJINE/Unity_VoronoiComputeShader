using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class MovingVoronoi : MonoBehaviour
{
    public struct Seed
    {
        public Vector2 coord;
        public Color   color;
        public Vector2 velocity;
    } 

    #region Field

    [SerializeField] private ComputeShader movingVoronoiComputeShader;
    [SerializeField] private Vector2Int    textureSize = new (512, 512);
    [SerializeField] private int           numSeeds    = 10;
    [SerializeField] private float         baseSpeed   = 0.0005f;

    private RenderTexture  _texture;
    private Seed[]         _seedBuffer;
    private ComputeBuffer  _seedBufferCompute;

    private int _kernelIndex_MovingVoronoi;
    private int _kernelIndex_UpdateSeeds;

    private Vector3Int _threadSize_MovingVoronoi;
    private Vector3Int _threadSize_UpdateSeeds;

    #endregion Field

    #region Property

    private static int PropertyID_Texture    { get; }
    private static int PropertyID_SeedBuffer { get; }
    private static int PropertyID_DeltaTime  { get; }

    #endregion Property

    #region Constructor

    static MovingVoronoi()
    {
        PropertyID_Texture    = Shader.PropertyToID("Texture");
        PropertyID_SeedBuffer = Shader.PropertyToID("SeedBuffer");
        PropertyID_DeltaTime  = Shader.PropertyToID("DeltaTime");
    }

    #endregion Constructor

    private void Start()
    {
        _texture = new RenderTexture(textureSize.x, textureSize.y, 0)
        {
            graphicsFormat    = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
            enableRandomWrite = true
        };
        _texture.Create();

        GenerateSeeds();

        _kernelIndex_MovingVoronoi = movingVoronoiComputeShader.FindKernel("MovingVoronoi");
        movingVoronoiComputeShader.GetKernelThreadGroupSizes(_kernelIndex_MovingVoronoi,
                                                             out var threadSizeX,
                                                             out var threadSizeY,
                                                             out var threadSizeZ);
        _threadSize_MovingVoronoi = new Vector3Int((int)threadSizeX, (int)threadSizeY, (int)threadSizeZ);

        _kernelIndex_UpdateSeeds = movingVoronoiComputeShader.FindKernel("UpdateSeeds");
        movingVoronoiComputeShader.GetKernelThreadGroupSizes(_kernelIndex_UpdateSeeds,
                                                             out threadSizeX,
                                                             out threadSizeY,
                                                             out threadSizeZ);
        _threadSize_UpdateSeeds = new Vector3Int((int)threadSizeX, (int)threadSizeY, (int)threadSizeZ);

        _seedBufferCompute = new ComputeBuffer(numSeeds, Marshal.SizeOf(typeof(Seed)));
        _seedBufferCompute.SetData(_seedBuffer);

        movingVoronoiComputeShader.SetTexture(_kernelIndex_MovingVoronoi, PropertyID_Texture,    _texture);
        movingVoronoiComputeShader.SetBuffer (_kernelIndex_MovingVoronoi, PropertyID_SeedBuffer, _seedBufferCompute);
        movingVoronoiComputeShader.SetBuffer (_kernelIndex_UpdateSeeds,   PropertyID_SeedBuffer, _seedBufferCompute);
    }

    private void Update()
    {
        movingVoronoiComputeShader.SetFloat(PropertyID_DeltaTime, Time.deltaTime);

        movingVoronoiComputeShader.Dispatch(_kernelIndex_UpdateSeeds,
                                            numSeeds,
                                            _threadSize_UpdateSeeds.y,
                                            _threadSize_UpdateSeeds.z);

        movingVoronoiComputeShader.Dispatch(_kernelIndex_MovingVoronoi,
                                            Mathf.CeilToInt(textureSize.x / (float)_threadSize_MovingVoronoi.x),
                                            Mathf.CeilToInt(textureSize.y / (float)_threadSize_MovingVoronoi.y),
                                                                                   _threadSize_MovingVoronoi.z);
    }

    private void GenerateSeeds()
    {
        _seedBuffer = new Seed[numSeeds];

        for (var i = 0; i < numSeeds; i++)
        {
            _seedBuffer[i] = new Seed()
            {
                coord    = new Vector2(Random.value, Random.value),
                color    = Random.ColorHSV(),
                velocity = Random.insideUnitCircle.normalized * baseSpeed,
            };
        }
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, textureSize.x, textureSize.y), _texture);
    }

    private void OnDisable()
    {
        Destroy(_texture);
        _seedBufferCompute.Dispose();
    }
}