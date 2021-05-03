using System;
using Godot;

[Tool]
public class Wheel : Spatial
{
    private Spatial _model { get; set; }
    private Godot.Collections.Array exclude { get; set; }

    public float suspensionTension { get; private set; }
    public float suspensionDistance { get; private set; }
    public bool colliding { get; private set; }
    public Vector3 contactPos { get; private set; }
    public Vector3 contactNormal { get; private set; }

    public Spatial model
    {
        get
        {
            if (_model == null || !_model.IsInsideTree())
            {
                _model = GetChildOrNull<Spatial>(0);
            }
            return _model;
        }
    }
    public float maxRayDistance
    {
        get
        {
            return maxRange + wheelRadius;
        }
    }

    [Export] public float minRange = .25f;
    [Export] public float maxRange = 1f;
    [Export] public float wheelRadius = .25f;
    [Export] public float previewFrequency = .25f;
    [Export] public float springDrag = 8f;
    [Export] public float springAngularDrag = 4f;
    [Export] public float forceMultiply = .25f;
    [Export(PropertyHint.Layers3dPhysics)] public uint collisionLayer = 1;

    public Wheel()
    {
        exclude = new Godot.Collections.Array();
    }

    public override void _Process(float delta)
    {
        if (model != null)
        {
            Transform t = Transform.Identity;

            if (Transform.origin.x > 0f)
            {
                Basis b = t.basis;
                Vector3 s = b.Scale;
                s.x *= -1f;

                b.Scale = s;
                t.basis = b;
            }

            model.Transform = t;

            if (Engine.EditorHint)
            {
                PreviewProcess(delta);
            }
            else
            {
                UpdateModel(delta);
            }
        }
    }

    // Just a simple method to preview the suspension length in editor
    private void PreviewProcess(float delta)
    {
        Transform t = model.Transform;
        Vector3 o = t.origin;

        float time = OS.GetTicksMsec() / 1000f;
        float sin = Mathf.Sin(time * Mathf.Pi * 2f * previewFrequency);

        o = Vector3.Down * Mathf.Lerp(minRange, maxRange, sin * .5f + .5f);

        t.origin = o;
        model.Transform = t;
    }

    private void UpdateModel(float delta)
    {
        Transform t = model.Transform;
        Vector3 o = t.origin;

        o = Vector3.Down * suspensionDistance;

        t.origin = o;
        model.Transform = t;
    }

    public void AddExclusion(Node obj)
    {
        exclude.Add(obj);
    }

    public void UpdateWheel(PhysicsDirectBodyState state)
    {
        RaycastProcess(state);
        ClampSuspension(state);
        SuspensionProcess(state);
    }

    private void RaycastProcess(PhysicsDirectBodyState state)
    {
        var spaceState = GetWorld().DirectSpaceState;

        Vector3 rayFrom = GlobalTransform.origin;
        Vector3 rayTo = rayFrom - GlobalTransform.basis.y * maxRayDistance;

        var ray = spaceState.IntersectRay(
            rayFrom, rayTo, exclude, collisionLayer
        );

        suspensionTension = 0f;
        suspensionDistance = maxRange;
        colliding = false;
        if (ray.Count > 0)
        {
            colliding = true;
            contactPos = (Vector3)ray["position"];
            contactNormal = (Vector3)ray["normal"];
            suspensionDistance = (contactPos - rayFrom).Length() - wheelRadius;
            suspensionTension = Mathf.InverseLerp(maxRange, minRange, suspensionDistance);
        }
    }

    private void ClampSuspension(PhysicsDirectBodyState state)
    {
        Vector3 relativePos = GlobalTransform.origin - state.Transform.origin;

        if (suspensionDistance < minRange)
        {
            Transform t = state.Transform;

            float diff = minRange - suspensionDistance;
            suspensionDistance += diff;
            suspensionTension = Mathf.InverseLerp(maxRange, minRange, suspensionDistance);

            Vector3 vel = contactNormal * contactNormal.Dot(state.LinearVelocity);

            state.LinearVelocity -= vel;
            state.ApplyImpulse(
                relativePos,
                GlobalTransform.basis.y * diff
            );

            t = t.Translated(GlobalTransform.basis.y * diff);
            state.Transform = t;
        }
    }

    private void SuspensionProcess(PhysicsDirectBodyState state)
    {
        Vector3 angDrag = GlobalTransform.basis.XformInv(state.AngularVelocity);

        angDrag.y = 0f;
        angDrag *= suspensionTension;

        angDrag = GlobalTransform.basis.Xform(angDrag);
        state.AngularVelocity -= angDrag * Mathf.Clamp(state.Step * springAngularDrag * forceMultiply, 0f, 1f);

        Vector3 verticalDrag = GlobalTransform.basis.y * GlobalTransform.basis.y.Dot(state.LinearVelocity);

        verticalDrag *= suspensionTension;

        state.LinearVelocity -= verticalDrag * Mathf.Clamp(state.Step * springDrag * forceMultiply, 0f, 1f);
    }
}
