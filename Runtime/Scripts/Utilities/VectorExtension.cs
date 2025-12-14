using UnityEngine;

namespace Majinfwork {
    public static class VectorExtension {
        private enum Axis { X, Y, Z, XY, XZ, YZ }
        public static Vector3 ExcludingX(this Vector3 self) => self.Excluding(Axis.X);
        public static Vector3 ExcludingY(this Vector3 self) => self.Excluding(Axis.Y);
        public static Vector3 ExcludingZ(this Vector3 self) => self.Excluding(Axis.Z);
        public static Vector3 ExcludingXY(this Vector3 self) => self.Excluding(Axis.XY);
        public static Vector3 ExcludingXZ(this Vector3 self) => self.Excluding(Axis.XZ);
        public static Vector3 ExcludingYZ(this Vector3 self) => self.Excluding(Axis.YZ);

        private static Vector3 Excluding(this Vector3 self, Axis axis) {
            switch (axis) {
                case Axis.X:
                    return new Vector3(0, self.y, self.z);
                case Axis.Y:
                    return new Vector3(self.x, 0, self.z);
                case Axis.Z:
                    return new Vector3(self.x, self.y, 0);
                case Axis.XY:
                    return new Vector3(0, 0, self.z);
                case Axis.XZ:
                    return new Vector3(0, self.y, 0);
                case Axis.YZ:
                    return new Vector3(self.x, 0, 0);
                default:
                    return self;
            }

        }

        public static Vector3 InvertX(this Vector3 self) => self.Invert(Axis.X);
        public static Vector3 InvertY(this Vector3 self) => self.Invert(Axis.Y);
        public static Vector3 InvertZ(this Vector3 self) => self.Invert(Axis.Z);
        public static Vector3 InvertXY(this Vector3 self) => self.Invert(Axis.XY);
        public static Vector3 InvertXZ(this Vector3 self) => self.Invert(Axis.XZ);
        public static Vector3 InvertYZ(this Vector3 self) => self.Invert(Axis.YZ);
        public static Vector3 InvertAll(this Vector3 self) => -self;

        private static Vector3 Invert(this Vector3 self, Axis axis) {
            switch (axis) {
                case Axis.X:
                    return new Vector3(-self.x, self.y, self.z);
                case Axis.Y:
                    return new Vector3(self.x, -self.y, self.z);
                case Axis.Z:
                    return new Vector3(self.x, self.y, -self.z);
                case Axis.XY:
                    return new Vector3(-self.x, -self.y, self.z);
                case Axis.XZ:
                    return new Vector3(-self.x, self.y, -self.z);
                case Axis.YZ:
                    return new Vector3(self.x, -self.y, -self.z);
                default:
                    return self;
            }
        }

        public static float GetRotationAngleX(this Vector3 self) => self.GetRotationAngle(Axis.X);
        public static float GetRotationAngleY(this Vector3 self) => self.GetRotationAngle(Axis.Y);
        public static float GetRotationAngleZ(this Vector3 self) => self.GetRotationAngle(Axis.Z);


        private static float GetRotationAngle(this Vector3 self, Axis axis) {
            //Returns the angle of the object's forward vector, as seen from the top
            Vector3 direction = self;

            float angleInDegrees = 0;

            switch (axis) {
                case Axis.X:
                    angleInDegrees = Mathf.Atan2(direction.y, direction.z) * Mathf.Rad2Deg;
                    break;
                case Axis.Y:
                    angleInDegrees = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
                    break;
                case Axis.Z:
                    angleInDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    break;
                case Axis.XY:
                    break;
                case Axis.XZ:
                    break;
                case Axis.YZ:
                    break;
                default:
                    break;
            }

            return angleInDegrees;
        }

        public static Vector3 InverseTransformPoint(this Vector3 self, Vector3 sourcePosition, Quaternion sourceRotation, Vector3 sourceScale) {
            Matrix4x4 matrix = Matrix4x4.TRS(sourcePosition, sourceRotation, sourceScale);
            Matrix4x4 inverse = matrix.inverse;
            return inverse.MultiplyPoint3x4(self);
        }

        public static Vector3 RotatePointAroundPivot(this Vector3 self, Vector3 pivot, Quaternion rotation) {
            Vector3 output = self;
            var dir = pivot - output;
            dir = rotation * dir;
            output = dir + pivot;
            return output;
        }

        /// <summary>
        /// Turn angle into vector direction
        /// </summary>
        /// <param name="angle">Horizontal degree value</param>
        /// <returns></returns>
        public static Vector3 HorizontalDirection(this float angle) {
            angle *= Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        }


        /// <summary>
        /// Turn vector direction into degree
        /// </summary>
        /// <param name="vector">Horizontal Direction</param>
        /// <returns></returns>
        public static float HorizontalAngle(this Vector3 vector) {
            var v = new Vector2(vector.z, vector.x);

            if (v.sqrMagnitude > 0.01f)
                v.Normalize();

            var sign = v.y < 0 ? -1.0f : 1.0f;
            return Vector2.Angle(Vector2.right, v) * sign;
        }
    }
}