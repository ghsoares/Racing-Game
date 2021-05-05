using Godot;
using System;

public class RoadManager : Spatial
{
    private RoadPath _roadPath {get; set;}
    private RoadMesh _roadMesh {get; set;}

    public RoadPath roadPath {
        get {
            if (_roadPath == null || !_roadPath.IsInsideTree()) {
                _roadPath = GetChild<RoadPath>(0);
            }
            return _roadPath;
        }
    }

    public RoadMesh roadMesh {
        get {
            if (_roadMesh == null || !_roadMesh.IsInsideTree()) {
                _roadMesh = GetChild<RoadMesh>(1);
            }
            return _roadMesh;
        }
    }

    public override void _Ready()
    {
        if (!Engine.EditorHint) Generate();
    }

    private void Generate() {
        if (roadPath != null && roadMesh != null) {
            roadPath.Generate();
            /*Vector3[] points = roadPath.Curve.Tessellate(
                5, 4f
            );
            roadMesh.Generate(points);*/
        }
    }
}
