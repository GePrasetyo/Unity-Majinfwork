using UnityEngine;

namespace Majinfwork {
    public static class QuaternionExtension {
        public static Quaternion IsolateXRotation(this Quaternion q) {
            float theta_x = Mathf.Atan2(q.x, q.w);

            Quaternion xRotation = new Quaternion(Mathf.Sin(theta_x), 0, 0, Mathf.Cos(theta_x));

            return xRotation;
        }

        public static Quaternion IsolateYRotation(this Quaternion q) {
            float theta_y = Mathf.Atan2(q.y, q.w);

            Quaternion yRotation = new Quaternion(0, Mathf.Sin(theta_y), 0, Mathf.Cos(theta_y));

            return yRotation;
        }

        public static Quaternion IsolateZRotation(this Quaternion q) {
            float theta_z = Mathf.Atan2(q.z, q.w);

            Quaternion zRotation = new Quaternion(0, 0, Mathf.Sin(theta_z), Mathf.Cos(theta_z));

            return zRotation;
        }

        public static Quaternion ClampRotationX(this Quaternion q, float angleInDegrees) {
            Quaternion output = q;

            output.x /= output.w;
            output.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(output.x);
            angleX = Mathf.Clamp(angleX, -angleInDegrees, angleInDegrees);
            output.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return output;
        }

        public static Quaternion ClampRotationY(this Quaternion q, float angleInDegrees) {
            Quaternion output = q;

            output.y /= output.w;
            output.w = 1.0f;

            float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(output.y);
            angleY = Mathf.Clamp(angleY, -angleInDegrees, angleInDegrees);
            output.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

            return output;
        }

        public static Quaternion ClampRotationZ(this Quaternion q, float angleInDegrees) {
            Quaternion output = q;

            output.z /= output.w;
            output.w = 1.0f;

            float angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(output.z);
            angleZ = Mathf.Clamp(angleZ, -angleInDegrees, angleInDegrees);
            output.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);

            return output;
        }

        public static Quaternion ClampRotationXYZ(this Quaternion q, Vector3 bounds) {
            Quaternion output = q;

            output.x /= output.w;
            output.y /= output.w;
            output.z /= output.w;
            output.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(output.x);
            angleX = Mathf.Clamp(angleX, -bounds.x, bounds.x);
            output.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(output.y);
            angleY = Mathf.Clamp(angleY, -bounds.y, bounds.y);
            output.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

            float angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(output.z);
            angleZ = Mathf.Clamp(angleZ, -bounds.z, bounds.z);
            output.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);

            return output;
        }
    }
}