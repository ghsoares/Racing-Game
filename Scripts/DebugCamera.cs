using System;
using Godot;

/*
    This node acts like the Godot's editor camera, where there's a pivot point
    that is moved and rotated by the user input, and the camera is relative to that pivot point,
    along with zoom changes
*/
public class DebugCamera : Camera
{
    private float cameraZoom { get; set; }
    private float desiredCameraZoom { get; set; }
    private Vector3 desiredAnchorPosition { get; set; }
    private Vector3 desiredAnchorRotation { get; set; }
    private bool dragging { get; set; }

    [Export] public float lerpSpeed = 8f;

    public override void _Ready()
    {
        desiredAnchorPosition = GlobalTransform.origin;
        desiredAnchorRotation = GlobalTransform.basis.Quat().GetEuler() / Mathf.Deg2Rad(1f);
        desiredCameraZoom = Far * .5f;
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventMouseButton)
        {
            InputEventMouseButton evMB = (InputEventMouseButton)ev;
            if (evMB.ButtonIndex == (int)ButtonList.Middle)
            {
                dragging = false;
                if (evMB.Pressed)
                {
                    dragging = true;
                }
            }
            else if (evMB.ButtonIndex == (int)ButtonList.WheelDown)
            {
                if (evMB.Pressed)
                {
                    desiredCameraZoom += 1f + desiredCameraZoom * .1f;
                }
            }
            else if (evMB.ButtonIndex == (int)ButtonList.WheelUp)
            {
                if (evMB.Pressed)
                {
                    desiredCameraZoom -= 1f + desiredCameraZoom * .1f;
                }
            }
        }

        if (ev is InputEventMouseMotion)
        {
            InputEventMouseMotion evMM = (InputEventMouseMotion)ev;

            if (dragging)
            {
                if (evMM.Shift)
                {
                    Vector3 localMotion = new Vector3(
                        evMM.Relative.x, evMM.Relative.y, 0f
                    ) * cameraZoom * 0.001f;
                    localMotion.y = -localMotion.y;
                    desiredAnchorPosition -= GlobalTransform.basis.Xform(localMotion);
                }
                else
                {
                    Vector3 rot = desiredAnchorRotation;

                    rot.y -= evMM.Relative.x * .25f;
                    rot.x -= evMM.Relative.y * .25f;

                    desiredAnchorRotation = rot;
                }
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector3 rot = desiredAnchorRotation;
        rot.x = Mathf.Clamp(rot.x, -90, 90f);

        desiredAnchorRotation = rot;

        Transform desiredT = Transform.Identity;
        desiredT.origin = desiredAnchorPosition;
        desiredT.basis = new Basis(new Quat(
            desiredAnchorRotation * Mathf.Deg2Rad(1f)
        ));

        float t =  Mathf.Clamp(lerpSpeed * delta, 0f, 1f);

        GlobalTransform = GlobalTransform.InterpolateWith(
            desiredT, t
        );

        desiredCameraZoom = Mathf.Clamp(desiredCameraZoom, Near * 4f, Far * .5f);
        cameraZoom = Mathf.Lerp(cameraZoom, desiredCameraZoom, t);
    }

    public override void _Process(float delta)
    {
        Transform t = Transform.Identity;
        Vector3 o = t.origin;

        o.z = cameraZoom;

        t.origin = o;
        t = GlobalTransform * t;

        VisualServer.CameraSetTransform(GetCameraRid(), t);
    }
}
