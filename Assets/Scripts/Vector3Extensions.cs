using UnityEngine;

namespace MeshSlices {
    public static class Vector3Extensions {
        public static Vector3 WithX(this Vector3 v, float x) {
            return new Vector3(x, v.y, v.z);
        }

        public static Vector3 WithY(this Vector3 v, float y) {
            return new Vector3(v.x, y, v.z);
        }

        public static Vector3 WithZ(this Vector3 v, float z) {
            return new Vector3(v.x, v.y, z);
        }

        public static Vector3 WithXY(this Vector3 v, float x, float y) {
            return new Vector3(x, y, v.z);
        }

        public static Vector3 WithYZ(this Vector3 v, float y, float z) {
            return new Vector3(v.x, y, z);
        }

        public static Vector3 WithZX(this Vector3 v, float z, float x) {
            return new Vector3(x, v.y, z);
        }

        public static Vector3 ToXZ(this Vector3 xy) {
            return new Vector3(xy.x, 0, xy.y);
        }
    }
}
