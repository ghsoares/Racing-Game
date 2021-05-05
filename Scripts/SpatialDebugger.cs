using System;
using System.Collections.Generic;
using Godot;

public class SpatialDebugger : Node
{
    private struct Line
    {
        public Vector3 a;
        public Vector3 b;
        public Color color;

        public Line(Vector3 a, Vector3 b, Color color)
        {
            this.a = a;
            this.b = b;
            this.color = color;
        }
    }

    public static SpatialDebugger instance { get; private set; }

    private ImmediateGeometry lineDrawer { get; set; }
    private List<Line> lines { get; set; }
    private bool queryLinesUpdate { get; set; }

    [Export] public Material debugMaterial;

    public SpatialDebugger()
    {
        instance = this;
        lineDrawer = new ImmediateGeometry();

        lines = new List<Line>();
    }

    public override void _Ready()
    {
        AddChild(lineDrawer);
        lineDrawer.MaterialOverride = debugMaterial;
    }

    public override void _Process(float delta)
    {
        if (queryLinesUpdate)
        {
            UpdateLines();
            queryLinesUpdate = false;
        }
    }

    public int AddLine(Vector3 a, Vector3 b, Color color)
    {
        lines.Add(new Line(a, b, color));

        queryLinesUpdate = true;

        return lines.Count - 1;
    }

    public void UpdateLine(int idx, Vector3 a, Vector3 b, Color color) {
        Line line = this.lines[idx];

        line.a = a;
        line.b = b;
        line.color = color;

        this.lines[idx] = line;

        queryLinesUpdate = true;
    }

    private void UpdateLines()
    {
        lineDrawer.Clear();

        lineDrawer.Begin(Mesh.PrimitiveType.Lines);

        foreach (Line l in lines)
        {
            lineDrawer.SetColor(l.color);
            lineDrawer.AddVertex(l.a);
            lineDrawer.AddVertex(l.b);
        }

        lineDrawer.End();
    }
}
