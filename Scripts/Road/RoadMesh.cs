using System;
using Godot;

public class RoadMesh : MeshInstance
{
    private SurfaceTool st { get; set; }
    private int numPoints { get; set; }
    private Vector3[] points { get; set; }
    private Vector3[] forwardVecs { get; set; }
    private Vector3[] rightVecs { get; set; }
    private float[] lengths { get; set; }
    private float totalLength { get; set; }

    [Export] public float roadWidth = 16f;
    [Export] public float capLength = 64f;
    [Export] public bool tileUV;
    [Export] public Vector2 UVScale = Vector2.One;

    public void Generate(Vector3[] fromPoints)
    {
        this.points = fromPoints;
        this.numPoints = fromPoints.Length;

        GetData();
        Mesh mesh = GenerateMesh();
        this.Mesh = mesh;

        foreach (Node n in GetChildren()) {
            if (n is StaticBody) n.QueueFree();
        }

        CreateTrimeshCollision();
    }

    private void GetData()
    {
        forwardVecs = new Vector3[numPoints];
        rightVecs = new Vector3[numPoints];
        lengths = new float[numPoints];

        Vector3 prevP = points[0];
        Vector3 currForward = Vector3.Zero;
        float currL = 0f;

        for (int i = 0; i < numPoints; i++)
        {
            if (i < numPoints - 1)
            {
                currForward = (points[i + 1] - points[i + 0]).Normalized();
            }

            float l = (prevP - points[i]).Length();
            currL += l;

            Vector3 right = currForward.Cross(Vector3.Up);
            forwardVecs[i] = currForward;
            rightVecs[i] = right;
            lengths[i] = currL;

            prevP = points[i];
        }

        totalLength = currL;
    }

    private Mesh GenerateMesh()
    {
        st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < numPoints - 1; i++)
        {
            Vector3 thisP = points[i + 0];
            Vector3 nxtP = points[i + 1];

            Vector3 thisR = rightVecs[i + 0];
            Vector3 nxtR = rightVecs[i + 1];

            float thisL = lengths[i + 0];
            float nxtL = lengths[i + 1];

            Vector3 vec1 = thisP + thisR * roadWidth * .5f;
            Vector3 vec2 = thisP - thisR * roadWidth * .5f;
            Vector3 vec3 = nxtP - nxtR * roadWidth * .5f;
            Vector3 vec4 = nxtP + nxtR * roadWidth * .5f;

            Vector2 uv1 = Vector2.Zero;
            Vector2 uv2 = Vector2.Zero;
            Vector2 uv3 = Vector2.Zero;
            Vector2 uv4 = Vector2.Zero;

            if (tileUV)
            {
                uv1 = new Vector2(thisL / roadWidth, 0f);
                uv2 = new Vector2(thisL / roadWidth, 1f);
                uv3 = new Vector2(nxtL / roadWidth, 1f);
                uv4 = new Vector2(nxtL / roadWidth, 0f);
            }
            else
            {
                uv1 = new Vector2(thisL / totalLength, 0f);
                uv2 = new Vector2(thisL / totalLength, 1f);
                uv3 = new Vector2(nxtL / totalLength, 1f);
                uv4 = new Vector2(nxtL / totalLength, 0f);
            }

            uv1 *= UVScale;
            uv2 *= UVScale;
            uv3 *= UVScale;
            uv4 *= UVScale;

            CreateTRI(
                vec1, vec2, vec4,
                uv1, uv2, uv4
            );
            CreateTRI(
                vec4, vec2, vec3,
                uv4, uv2, uv3
            );
        }

        AddCaps();

        st.Index();
        st.GenerateNormals();
        return st.Commit();
    }

    private void AddCaps()
    {
        Vector3 startVec = points[0];
        Vector3 endVec = points[numPoints - 1];

        Vector3 startRight = rightVecs[0];
        Vector3 endRight = rightVecs[numPoints - 1];

        Vector3 startForward = forwardVecs[0];
        Vector3 endForward = forwardVecs[numPoints - 1];

        Vector3 capStartVec = startVec - startForward * capLength;
        Vector3 capEndVec = endVec + endForward * capLength;

        // First cap
        Vector3 vec1 = capStartVec + startRight * roadWidth * .5f;
        Vector3 vec2 = capStartVec - startRight * roadWidth * .5f;
        Vector3 vec3 = startVec - startRight * roadWidth * .5f;
        Vector3 vec4 = startVec + startRight * roadWidth * .5f;

        Vector2 uv1 = new Vector2(-capLength / roadWidth, 0f);
        Vector2 uv2 = new Vector2(-capLength / roadWidth, 1f);
        Vector2 uv3 = new Vector2(0f, 1f);
        Vector2 uv4 = new Vector2(0f, 0f);

        uv1 *= UVScale;
        uv2 *= UVScale;
        uv3 *= UVScale;
        uv4 *= UVScale;

        CreateTRI(
            vec1, vec2, vec4,
            uv1, uv2, uv4
        );
        CreateTRI(
            vec4, vec2, vec3,
            uv4, uv2, uv3
        );

        // Second cap
        vec1 = endVec + endRight * roadWidth * .5f;
        vec2 = endVec - endRight * roadWidth * .5f;
        vec3 = capEndVec - endRight * roadWidth * .5f;
        vec4 = capEndVec + endRight * roadWidth * .5f;

        uv1 = new Vector2(totalLength / roadWidth, 0f);
        uv2 = new Vector2(totalLength / roadWidth, 1f);
        uv3 = new Vector2((totalLength + capLength) / roadWidth, 1f);
        uv4 = new Vector2((totalLength + capLength) / roadWidth, 0f);

        uv1 *= UVScale;
        uv2 *= UVScale;
        uv3 *= UVScale;
        uv4 *= UVScale;

        CreateTRI(
            vec1, vec2, vec4,
            uv1, uv2, uv4
        );
        CreateTRI(
            vec4, vec2, vec3,
            uv4, uv2, uv3
        );
    }

    private void CreateTRI(
        Vector3 vec1, Vector3 vec2, Vector3 vec3,
        Vector2 uv1, Vector2 uv2, Vector2 uv3
    )
    {
        st.AddUv(uv1);
        st.AddVertex(vec1);
        st.AddUv(uv2);
        st.AddVertex(vec2);
        st.AddUv(uv3);
        st.AddVertex(vec3);
    }
}
