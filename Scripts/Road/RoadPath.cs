using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class RoadPath : Path
{
    public Random rng { get; private set; }

    private float delay = 0f;
    private RoadWalker walker;

    [Export] public int numPoints = 8;
    [Export] public int iterations = 1000;
    [Export] public float initialRadius = 4f;
    [Export] public float smoothWeight = .25f;
    [Export] public OpenSimplexNoise elevationNoise;
    [Export] public float elevationFrequency = 64f;
    [Export] public Vector2 elevationRange = new Vector2(-16f, 64f);
    [Export] public float elevationCorrection = .5f;
    [Export] public float elevationDiffThreshold = 1f;
    [Export] public PackedScene testScene;

    public void Generate()
    {
        rng = new Random(137);

        GeneratePoints();
        SmoothCurve();

        /*float spacing = 1f / 512f;
        float len = Curve.GetBakedLength();

        for (float t = 0f; t <= 1f; t += spacing) {
            AddPoint(Curve.InterpolateBaked(t * len));
        }*/
    }

    private async void GeneratePoints()
    {
        Terrain terrain = Terrain.instance;

        for (int i = 0; i < numPoints; i++)
        {
            float t = (float)i / (numPoints);
            float a = t * Mathf.Pi * 2f;

            Vector2 p = Vector2.Right.Rotated(a) * initialRadius;
            Vector3 p3D = new Vector3(p.x, 0f, p.y);
            Vector3 normal = terrain.GetNormal(p3D);
            p3D.y = terrain.GetHeight(p3D);
            Curve.AddPoint(p3D);
            SpatialDebugger.instance.AddLine(p3D, p3D + normal * 16f, Colors.Blue);
            //viewPoints[i] = AddPoint(p3D);
        }

        for (int j = 0; j < iterations; j++)
        {
            bool moved = false;
            for (int i = 0; i < numPoints; i++)
            {
                float t = (float)i / (numPoints - 1);

                Vector3 p = Curve.GetPointPosition(i);
                Vector3 normal = terrain.GetNormal(p);
                Vector3 terrainNormal = normal;
                normal.y = 0f;
                normal = normal.Normalized();

                float desiredHeight = SampleNoise1D(
                    elevationNoise, t * elevationFrequency, elevationFrequency
                ) * .5f + .5f;
                desiredHeight = Mathf.Lerp(elevationRange.x, elevationRange.y, desiredHeight);

                float diff = desiredHeight - p.y;

                float diffT = 1f - (j % 100) / 100f;
                diffT = Mathf.Pow(diffT, .25f);
                diff *= diffT;

                if (diff <= elevationDiffThreshold) continue;

                p.x -= normal.x * diff * elevationCorrection;
                p.z -= normal.z * diff * elevationCorrection;

                if ((j + 1) % 100 == 0) {
                    float angle = (float)rng.NextDouble() * Mathf.Pi * 4f;
                    Vector2 dir = Vector2.Right.Rotated(angle);
                    p.x += dir.x * 256f;
                    p.z += dir.y * 256f;
                }

                p.y = terrain.GetHeight(p);
                Curve.SetPointPosition(i, p);

                SpatialDebugger.instance.UpdateLine(
                    i, p, p + terrainNormal * 16f, Colors.Blue
                );

                /*Transform tr = viewPoints[i].GlobalTransform;
                tr.origin = p;
                tr.basis = LookAtBasis(terrainNormal);
                viewPoints[i].GlobalTransform = tr;*/

                moved = true;
                
                if (j % 50 == 0) await ToSignal(GetTree(), "idle_frame");
            }

            if (!moved) continue;
            else System.Console.WriteLine("Next iteration");
        }
    
        System.Console.WriteLine("Ended basic");

        walker = new RoadWalker();
        walker.origin = Curve.GetPointPosition(0);
        Vector3 targetPoint = Curve.GetPointPosition(1);
        walker.direction = new Vector2(
            targetPoint.x - walker.origin.x,
            targetPoint.z - walker.origin.z
        ).Angle();

        for (int i = 0; i < numPoints; i++) {
            int nxt = (i + 1) % numPoints;
            await PathFind(Curve.GetPointPosition(nxt));
        }
    }

    private async Task PathFind(Vector3 b)
    {
        Terrain terrain = Terrain.instance;

        while (true) {
            walker.Walk(b);

            Vector3 p1 = walker.origin;
            Vector3 p2 = p1 + terrain.GetNormal(walker.origin) * 16f;

            SpatialDebugger.instance.AddLine(p1, p2, Colors.Red);

            float distance = (walker.origin - b).Length();
            if (distance <= walker.lookSpacing) break;

            await ToSignal(GetTree().CreateTimer(.1f), "timeout");
        }

        System.Console.WriteLine("Finished!");
    }

    private Basis LookAtBasis(Vector3 dir)
    {
        return Transform.Identity.LookingAt(-dir, Vector3.Up).basis;
    }

    private float SampleNoise1D(OpenSimplexNoise noise, float x, float w)
    {
        float curr = noise.GetNoise1d(x) * (w - x);
        float prev = noise.GetNoise1d(x - w) * (x);

        return (curr + prev) / w;
        /*
        F_tile(x, y, w, h) = (
        F(x, y) * (w - x) * (h - y) +
        F(x - w, y) * (x) * (h - y) +
        F(x - w, y - h) * (x) * (y) +
        F(x, y - h) * (w - x) * (y)
        ) / (w * h)
        */
    }

    private Spatial AddPoint(Vector3 p)
    {
        Spatial pS = testScene.Instance<Spatial>();
        AddChild(pS);

        Transform t = pS.GlobalTransform;
        t.origin = p;
        pS.GlobalTransform = t;

        return pS;
    }

    private void SmoothCurve()
    {
        int pointCount = Curve.GetPointCount();
        if (pointCount > 2)
        {
            for (int i = 0; i < pointCount; i++)
            {
                SmoothAnchor(i);
            }
        }
    }

    private void SmoothAnchor(int anchorIndex)
    {
        int pointCount = Curve.GetPointCount();

        // Calculate a vector that is perpendicular to the vector bisecting the angle between this anchor and its two immediate neighbours
        // The control points will be placed along that vector
        Vector3 anchorPos = Curve.GetPointPosition(anchorIndex);
        Vector3 dir = Vector3.Zero;
        float[] neighbourDistances = new float[2];

        if (anchorIndex - 1 >= 0)
        {
            Vector3 offset = Curve.GetPointPosition(anchorIndex - 1) - anchorPos;
            dir += offset.Normalized();
            neighbourDistances[0] = offset.Length();
        }
        if (anchorIndex + 1 <= pointCount - 1)
        {
            Vector3 offset = Curve.GetPointPosition(anchorIndex + 1) - anchorPos;
            dir -= offset.Normalized();
            neighbourDistances[1] = -offset.Length();
        }

        dir = dir.Normalized();

        Curve.SetPointIn(anchorIndex, dir * neighbourDistances[0] * smoothWeight);
        Curve.SetPointOut(anchorIndex, dir * neighbourDistances[1] * smoothWeight);
    }
}
