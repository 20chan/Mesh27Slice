using UnityEngine;

namespace MeshSlices {
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class MeshSlice : MonoBehaviour {
        public Mesh originalMesh;
        public Mesh instance;

        public Bounds originalBounds;
        public float x0, x1, y0, y1, z0, z1;
        public float x2, x3, y2, y3, z2, z3;

        public float controlMargin = 0.001f;
        public float controlSize = 0.3f;
        public float slicePanelPadding = 0.1f, slicePanelMultiplier = 1f;
        public Color controlColor = new Color(.17f, .11f, .67f);
        public Color controlOverColor = new Color(.9f, 0, 0);
        public Color controlDownColor = new Color(.56f, .1f, .1f);

        public EditorMode mode;
        public bool axisX, axisY, axisZ;

        public bool interactable;

        public Vector3 v0 {
            get => new Vector3(x0, y0, z0);
            set {
                x0 = value.x;
                y0 = value.y;
                z0 = value.z;
            }
        }
        public Vector3 v1 {
            get => new Vector3(x1, y1, z1);
            set {
                x1 = value.x;
                y1 = value.y;
                z1 = value.z;
            }
        }
        public Vector3 v2 {
            get => new Vector3(x2, y2, z2);
            set {
                x2 = value.x;
                y2 = value.y;
                z2 = value.z;
            }
        }
        public Vector3 v3 {
            get => new Vector3(x3, y3, z3);
            set {
                x3 = value.x;
                y3 = value.y;
                z3 = value.z;
            }
        }

        private MeshFilter _mf;
        private MeshFilter mf => _mf == null ? _mf = GetComponent<MeshFilter>() : _mf;
        private MeshRenderer _mr;
        private MeshRenderer mr => _mr == null ? _mr = GetComponent<MeshRenderer>() : _mr;

        private Vector3[] origVerts, verts, origNormals, normals;

        public void Init() {
            if (instance != null) {
                DestroyImmediate(instance);
            }

            origVerts = originalMesh.vertices;
            verts = originalMesh.vertices;
            origNormals = originalMesh.normals;
            normals = originalMesh.normals;
            instance = new Mesh {
                vertices = verts,
                normals = normals,
                colors = originalMesh.colors,
                uv = originalMesh.uv,
                triangles = originalMesh.triangles,
                tangents = originalMesh.tangents,
            };

            originalBounds = instance.bounds;
            x0 = x2 = originalBounds.min.x;
            y0 = y2 = originalBounds.min.y;
            z0 = z2 = originalBounds.min.z;
            x1 = x3 = originalBounds.max.x;
            y1 = y3 = originalBounds.max.y;
            z1 = z3 = originalBounds.max.z;

            var sub = originalMesh.subMeshCount;
            instance.subMeshCount = sub;
            for (var t = 0; t < sub; t++) {
                instance.SetTriangles(originalMesh.GetTriangles(t), t);
            }

            mf.sharedMesh = instance;
        }

        public void Slice() {
            for (var i = 0; i < verts.Length; i++) {
                V(origVerts[i], ref verts[i]);
            }

            void V(Vector3 source, ref Vector3 dest) {
                dest.x = X(source.x);
                dest.y = Y(source.y);
                dest.z = Z(source.z);
            }

            instance.vertices = verts;
        }

        public float X(float value) => S(value, x0, x1, x2, x3);
        public float Y(float value) => S(value, y0, y1, y2, y3);
        public float Z(float value) => S(value, z0, z1, z2, z3);

        public float S(float value, float v0, float v1, float v2, float v3){
            if (value < v0) {
                return value + v2 - v0;
            } else if (value < v1) {
                return Mathf.Lerp(v2, v3, Mathf.InverseLerp(v0, v1, value));
            } else {
                return value + v3 - v1;
            }
        }

        private void Update() {
            if (interactable) {
                Slice();
            }
        }
    }

    public enum EditorMode {
        Slice,
        Scale,
    }
}