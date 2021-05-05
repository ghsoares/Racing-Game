using Godot;

namespace ExtensionMethods {
    public static class RigidBodyExtensionMethods {
        public static Vector3 GetVelocityAtPoint(
            this PhysicsDirectBodyState bodyState, Vector3 worldPoint
        ) {
            Vector3 centeredPoint = worldPoint - bodyState.Transform.origin;

            Quat rotQuat = new Quat(bodyState.AngularVelocity);

            Transform motionT = new Transform(rotQuat, bodyState.LinearVelocity);
            Vector3 movedPoint = motionT.Xform(centeredPoint);

            return movedPoint - centeredPoint;
        }
    }
}