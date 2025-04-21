using UnityEngine;

public class VoronoiComputeShader : MonoBehaviour
{
    #region Field

    [SerializeField] private ComputeShader voronoiComputeShader;

    public Vector2Int textureSize = new (512, 512);
    public int        numSeeds    = 10;

    private RenderTexture _voronoiTexture;
    private Vector2[]     _seeds;
    private Color[]       _seedColors;

    #endregion Field

    #region Property

    private static int PropertyID_VoronoiTexture { get; }
    private static int PropertyID_SeedBuffer     { get; }
    private static int PropertyID_SeedColors     { get; }

    #endregion Property

    #region Constructor

    static VoronoiComputeShader()
    {
        PropertyID_VoronoiTexture = Shader.PropertyToID("VoronoiTexture");
        PropertyID_SeedBuffer     = Shader.PropertyToID("SeedBuffer");
        PropertyID_SeedColors     = Shader.PropertyToID("SeedColors");
    }

    #endregion Constructor


    private void Start()
    {
        _voronoiTexture = new RenderTexture(textureSize.x, textureSize.y, 0)
        {
            graphicsFormat    = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
            enableRandomWrite = true
        };
        _voronoiTexture.Create();

        GenerateSeeds();

        var kernelIndex = voronoiComputeShader.FindKernel("Voronoi");
        voronoiComputeShader.GetKernelThreadGroupSizes(kernelIndex, out var threadSizeX,
                                                                    out var threadSizeY,
                                                                    out var threadSizeZ);

        var seedBuffer = new ComputeBuffer(numSeeds, sizeof(float) * 2);
            seedBuffer.SetData(_seeds);

        var seedColors = new ComputeBuffer(numSeeds, sizeof(float) * 4);
            seedColors.SetData(_seedColors);

        voronoiComputeShader.SetTexture(kernelIndex, PropertyID_VoronoiTexture, _voronoiTexture);
        voronoiComputeShader.SetBuffer (kernelIndex, PropertyID_SeedBuffer,   seedBuffer);
        voronoiComputeShader.SetBuffer (kernelIndex, PropertyID_SeedColors,   seedColors);

        voronoiComputeShader.Dispatch(kernelIndex,
                                      Mathf.CeilToInt(textureSize.x / (float)threadSizeX),
                                      Mathf.CeilToInt(textureSize.y / (float)threadSizeY),
                                      (int)threadSizeZ);

        seedBuffer.Dispose();
        seedColors.Dispose();
    }

    private void GenerateSeeds()
    {
        _seeds      = new Vector2[numSeeds];
        _seedColors = new Color  [numSeeds];

        for (var i = 0; i < numSeeds; i++)
        {
            _seeds[i]      = new Vector2(Random.value, Random.value);
            _seedColors[i] = Random.ColorHSV();
        }
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, textureSize.x, textureSize.y), _voronoiTexture);
    }

    private void OnDisable()
    {
        Destroy(_voronoiTexture);
    }
}