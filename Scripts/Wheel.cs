using System;
using ExtensionMethods;
using Godot;

[Tool]
public class Wheel : Spatial
{
    private Spatial _model { get; set; }
    private Godot.Collections.Array exclude { get; set; }
    private PhysicsDirectBodyState bodyState { get; set; }
    private float currentForwardSlip {get; set;}
    private float currentSideSlip {get; set;}

    public float suspensionTension { get; private set; }
    public float suspensionDistance { get; private set; }
    public bool colliding { get; private set; }
    public Vector3 contactPos { get; private set; }
    public Vector3 contactNormal { get; private set; }
    public float rearWheelOff { get; set; }
    public float torque { get; set; }
    public float steeringAngle { get; set; }
    public float sideFrictionMultiply { get; set; }

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
    [Export] public float springForce = 100f;
    [Export] public float springDrag = 8f;
    [Export] public float springAngularDrag = 4f;
    [Export] public float springSteerDrag = 1f;
    [Export] public float sideFriction = 2f;
    [Export] public float forwardFriction = 1f;
    [Export] public Curve sideFrictionCurve;
    [Export] public Curve forwardFrictionCurve;
    [Export] public float sideFrictionTransfer = .5f;
    [Export] public float sideSlipMax = 32f;
    [Export] public float forwardSlipMax = 16f;
    [Export] public float forceMultiply = .25f;
    [Export] public float maxSteerForce = .75f;
    [Export] public bool motor;
    [Export] public bool steer;
    [Export(PropertyHint.Layers3dPhysics)] public uint collisionLayer = 1;

    public Wheel()
    {
        exclude = new Godot.Collections.Array();
        sideFrictionMultiply = 1f;
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
        if (steer) t.basis = new Basis(new Quat(new Vector3(0f, -steeringAngle, 0f))) * t.basis;
        model.Transform = t;
    }

    public void AddExclusion(Node obj)
    {
        exclude.Add(obj);
    }

    public void UpdateWheel(PhysicsDirectBodyState state)
    {
        bodyState = state;

        RaycastProcess();
        ClampSuspension();
        SuspensionProcess();
        DragProcess();
        if (motor) TorqueProcess();
        if (steer) SteerProcess();
    }

    private void RaycastProcess()
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

    private void ClampSuspension()
    {
        Vector3 relativePos = GlobalTransform.origin - bodyState.Transform.origin;

        if (suspensionDistance < minRange)
        {
            Transform t = bodyState.Transform;
            Vector3 o = t.origin;

            float diff = minRange - suspensionDistance;
            suspensionDistance += diff;
            suspensionTension = Mathf.InverseLerp(maxRange, minRange, suspensionDistance);

            Vector3 vel = contactNormal * contactNormal.Dot(bodyState.LinearVelocity);

            bodyState.LinearVelocity -= vel;
            bodyState.ApplyImpulse(
                relativePos,
                GlobalTransform.basis.y * diff
            );
            o += GlobalTransform.basis.y * diff;

            t.origin = o;
            bodyState.Transform = t;
        }
    }

    private void SuspensionProcess()
    {
        Vector3 relativePos = GlobalTransform.origin - bodyState.Transform.origin;

        bodyState.AddForce(
            GlobalTransform.basis.y * springForce * suspensionTension * forceMultiply,
            relativePos
        );

        Vector3 angDrag = GlobalTransform.basis.XformInv(bodyState.AngularVelocity);

        angDrag.y *= springSteerDrag;
        angDrag.x *= springAngularDrag;
        angDrag.z *= springAngularDrag;
        angDrag *= suspensionTension;

        angDrag = GlobalTransform.basis.Xform(angDrag);
        bodyState.AngularVelocity -= angDrag * Mathf.Clamp(bodyState.Step * forceMultiply, 0f, 1f);

        Vector3 verticalDrag = GlobalTransform.basis.y * GlobalTransform.basis.y.Dot(bodyState.LinearVelocity);

        verticalDrag *= suspensionTension;

        bodyState.LinearVelocity -= verticalDrag * Mathf.Clamp(bodyState.Step * springDrag * forceMultiply, 0f, 1f);
    }

    private void DragProcess()
    {
        Basis basis = GlobalTransform.basis;
        Vector3 localVelocity = bodyState.GetVelocityAtPoint(contactPos);
        Vector3 relativeHitPos = contactPos - bodyState.Transform.origin;

        float forwDrag = basis.z.Dot(localVelocity);
        float sideDrag = basis.x.Dot(localVelocity);

        float forwSlip = Mathf.Abs(forwDrag) / forwardSlipMax;
        float sideSlip = Mathf.Abs(sideDrag) / sideSlipMax;

        currentForwardSlip = Mathf.Lerp(
            currentForwardSlip, forwSlip, Mathf.Clamp(bodyState.Step * 2f, 0f, 1f)
        );
        currentSideSlip = Mathf.Lerp(
            currentSideSlip, sideSlip, Mathf.Clamp(bodyState.Step * 2f, 0f, 1f)
        );

        if (forwardFrictionCurve != null)
        {
            forwDrag *= forwardFrictionCurve.Interpolate(
                Mathf.Clamp(forwSlip, 0f, 1f)
            );
        }
        if (sideFrictionCurve != null)
        {
            sideDrag *= sideFrictionCurve.Interpolate(
                Mathf.Clamp(sideSlip, 0f, 1f)
            );
        }

        forwDrag *= forwardFriction * suspensionTension * forceMultiply;
        sideDrag *= sideFriction * suspensionTension * forceMultiply * sideFrictionMultiply;

        if (Transform.origin.x > 0f) {
            System.Console.WriteLine($"Side Slip: {sideSlip.ToString("0.00")}");
        }

        bodyState.AddForce(
            -basis.z * forwDrag,
            relativeHitPos
        );
        bodyState.AddForce(
            -basis.x * sideDrag,
            relativeHitPos
        );
        bodyState.AddForce(
            basis.z * Mathf.Abs(sideDrag) * sideFrictionTransfer,
            relativeHitPos
        );
    }

    private void TorqueProcess()
    {
        Vector3 relativeHitPos = contactPos - bodyState.Transform.origin;
        /*bodyState.AddForce(
            GlobalTransform.basis.z * torque * suspensionTension,
            relativeHitPos
        );*/
        bodyState.AddCentralForce(
            GlobalTransform.basis.z * torque * suspensionTension
        );
    }

    private void SteerProcess()
    {
        Vector3 velocityAtPoint = bodyState.GetVelocityAtPoint(contactPos);
        Vector3 localVel = GlobalTransform.basis.XformInv(velocityAtPoint);
        localVel = Vector3.Forward * Vector3.Forward.Dot(localVel);
        Quat steerQ = new Quat(new Vector3(0f, steeringAngle, 0f));

        Vector3 front = Transform.origin;
        front.x = 0f;
        Vector3 rear = front + Vector3.Forward * rearWheelOff;

        rear += localVel * bodyState.Step;
        front += steerQ.Xform(localVel) * bodyState.Step;

        Vector3 heading = (front - rear).Normalized();

        float angleDelta = Mathf.Rad2Deg(heading.AngleTo(Vector3.Forward));
        angleDelta *= Mathf.Sign(heading.Dot(-Vector3.Right)) * .5f;

        angleDelta = Mathf.Clamp(angleDelta, -maxSteerForce, maxSteerForce);
        //System.Console.WriteLine(angleDelta);

        bodyState.AddTorque(
            bodyState.Transform.basis.y * angleDelta * suspensionTension
        );
    }
}
