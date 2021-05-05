using System.Collections.Generic;
using Godot;

public class Terrain : Spatial
{
    private List<System.Threading.Thread> chunkThreads { get; set; }

    public static Terrain instance { get; private set; }

    public Dictionary<Vector2, MeshInstance> chunks { get; set; }

    [Export] public OpenSimplexNoise baseNoise;
    [Export] public OpenSimplexNoise mountainNoise;
    [Export] public Curve mountainCurve;
    [Export] public Vector2 heightRange = new Vector2(-32f, 64f);
    [Export] public Vector2 chunkSize = new Vector2(256f, 256f);
    [Export] public float cameraViewRadius = 512f;
    [Export] public int subdivision = 0;
    [Export] public ShaderMaterial material;

    public Terrain()
    {
        chunkThreads = new List<System.Threading.Thread>();
        instance = this;
        chunks = new Dictionary<Vector2, MeshInstance>();
    }

    public override void _PhysicsProcess(float delta)
    {
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        Camera cam = GetViewport().GetCamera();
        Vector3 camPos = cam.GlobalTransform.origin;

        GetChunkIdx(camPos, out int idxX, out int idxY);
        int rangeX = Mathf.CeilToInt(cameraViewRadius / chunkSize.x);
        int rangeY = Mathf.CeilToInt(cameraViewRadius / chunkSize.y);

        foreach (MeshInstance c in chunks.Values)
        {
            c.Hide();
        }

        for (int x = -rangeX; x <= rangeX; x++)
        {
            for (int y = -rangeX; y <= rangeY; y++)
            {
                Vector2 chunkIdx = new Vector2(x + idxX, y + idxY);
                if (chunks.ContainsKey(chunkIdx))
                {
                    chunks[chunkIdx].Show();
                }
                else
                {
                    //CreateChunk(chunkIdx);
                    MeshInstance chunk = CreateChunk(chunkIdx);
                    chunks[chunkIdx] = chunk;
                    AddChild(chunk);
                }
            }
        }
    }

    public void GetChunkIdx(Vector2 pos, out int idxX, out int idxY)
    {
        idxX = Mathf.FloorToInt(pos.x / chunkSize.x);
        idxY = Mathf.FloorToInt(pos.y / chunkSize.y);
    }

    public void GetChunkIdx(Vector3 pos, out int idxX, out int idxY)
    {
        idxX = Mathf.FloorToInt(pos.x / chunkSize.x);
        idxY = Mathf.FloorToInt(pos.z / chunkSize.y);
    }

    public float GetHeight(Vector3 pos)
    {
        float noiseN = baseNoise.GetNoise2d(pos.x, pos.z) * .5f + .5f;
        float mountainN = mountainNoise.GetNoise2d(pos.x, pos.z) * .5f + .5f;

        if (mountainCurve != null)
        {
            mountainN = mountainCurve.Interpolate(mountainN);
        }

        float h = noiseN * mountainN;

        return Mathf.Lerp(heightRange.x, heightRange.y, h);
    }

    public Vector3 GetNormal(Vector3 pos)
    {
        float hc = GetHeight(pos);
        float hx = GetHeight(pos + Vector3.Right);
        float hz = GetHeight(pos + Vector3.Forward);

        Vector3 n = new Vector3(
            -(hx - hc),
            1f,
            (hz - hc)
        );

        return n.Normalized();
    }

    private MeshInstance CreateChunk(Vector2 chunkIdx)
    {
        MeshInstance meshInstance = new MeshInstance();

        Vector3 pos = new Vector3(
            chunkIdx.x * chunkSize.x, 0f, chunkIdx.y * chunkSize.y
        );

        System.Threading.Tasks.Task.Run(() => {
            Mesh mesh = GenerateMesh(pos, chunkSize, this.subdivision);
            meshInstance.Mesh = mesh;
        });

        meshInstance.MaterialOverride = material;
        meshInstance.Translation = pos;

        return meshInstance;
    }

    private Mesh GenerateMesh(Vector3 pos, Vector2 size, int subdivision = 0)
    {
        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        int verts = 2 + subdivision;
        Vector3[,] vertices = new Vector3[verts, verts];

        for (int i = 0; i < verts; i++)
        {
            for (int j = 0; j < verts; j++)
            {
                Vector3 p = new Vector3(
                    ((float)i / (verts - 1)),
                    0f,
                    ((float)j / (verts - 1))
                );

                p.x *= size.x;
                p.z *= size.y;
                p.y = GetHeight(p + pos);

                vertices[i, j] = p;
            }
        }

        for (int i = 0; i < verts - 1; i++)
        {
            for (int j = 0; j < verts - 1; j++)
            {
                st.AddVertex(vertices[i + 1, j + 0]);
                st.AddVertex(vertices[i + 1, j + 1]);
                st.AddVertex(vertices[i + 0, j + 0]);

                st.AddVertex(vertices[i + 0, j + 0]);
                st.AddVertex(vertices[i + 1, j + 1]);
                st.AddVertex(vertices[i + 0, j + 1]);
            }
        }

        st.GenerateNormals();
        //st.GenerateTangents();
        st.Index();
        return st.Commit();
    }
}
