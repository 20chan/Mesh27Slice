using MeshSlices;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshSlice))]
public class MeshSliceEditor : Editor {
    private MeshSlice slice;

    private int[] controlIds;
    private bool isDragging;
    private int draggingId;
    private float downOffset;

    private ref EditorMode mode => ref slice.mode;
    private ref bool axisX => ref slice.axisX;
    private ref bool axisY => ref slice.axisY;
    private ref bool axisZ => ref slice.axisZ;

    private static Vector3[] rectCache = new Vector3[4];
    private GUIStyle buttonStyle, pressedButtonStyle;

    private SerializedProperty marginProp, sizeProp, padProp, multProp;
    private SerializedProperty controlProp, overProp, downProp;
    private bool foldOptions;

    private void OnEnable() {
        slice = target as MeshSlice;
        controlIds = new int[6];

        for (var i = 0; i < 6; i++) {
            controlIds[i] = GUIUtility.GetControlID(740 + i, FocusType.Passive);
        }

        marginProp = serializedObject.FindProperty("controlMargin");
        sizeProp = serializedObject.FindProperty("controlSize");
        padProp = serializedObject.FindProperty("slicePanelPadding");
        multProp = serializedObject.FindProperty("slicePanelMultiplier");
        controlProp = serializedObject.FindProperty("controlColor");
        overProp = serializedObject.FindProperty("controlOverColor");
        downProp = serializedObject.FindProperty("controlDownColor");
    }

    public override void OnInspectorGUI() {
        if (GUILayout.Button("Load")) {
            slice.originalMesh = slice.GetComponent<MeshFilter>().sharedMesh;
            slice.Init();
        }

        using (var check = new EditorGUI.ChangeCheckScope()) {
            slice.originalMesh = (Mesh)EditorGUILayout.ObjectField("mesh", slice.originalMesh, typeof(Mesh), false);
            if (check.changed) {
                slice.Init();
                SetSliceDirty();
            }
        }

        if (buttonStyle == null) {
            buttonStyle = new GUIStyle(GUI.skin.button);
            pressedButtonStyle = new GUIStyle(GUI.skin.button);
            pressedButtonStyle.normal = buttonStyle.active;
            pressedButtonStyle.normal.textColor = Color.red;
            pressedButtonStyle.active = buttonStyle.normal;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("SLICE", mode != EditorMode.Slice ? buttonStyle : pressedButtonStyle)) {
            mode = EditorMode.Slice;
        }
        if (GUILayout.Button("SCALE", mode != EditorMode.Scale ? buttonStyle : pressedButtonStyle)) {
            mode = EditorMode.Scale;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        axisX = GUILayout.Toggle(axisX, "X", "button");
        axisY = GUILayout.Toggle(axisY, "Y", "button");
        axisZ = GUILayout.Toggle(axisZ, "Z", "button");
        EditorGUILayout.EndHorizontal();

        if (mode == EditorMode.Slice) {
            using (var check = new EditorGUI.ChangeCheckScope()) {
                Axis("X", ref slice.x0, ref slice.x1);
                Axis("Y", ref slice.y0, ref slice.y1);
                Axis("Z", ref slice.z0, ref slice.z1);

                if (check.changed) {
                    SetSliceDirty();
                }
            }
        } else {
            using (var check = new EditorGUI.ChangeCheckScope()) {
                Axis("X", ref slice.x2, ref slice.x3);
                Axis("Y", ref slice.y2, ref slice.y3);
                Axis("Z", ref slice.z2, ref slice.z3);

                if (check.changed) {
                    slice.Slice();
                    SetSliceDirty();
                }
            }
        }

        foldOptions = EditorGUILayout.BeginFoldoutHeaderGroup(foldOptions, "Editor Options");
        if (foldOptions) {

            using (var check = new EditorGUI.ChangeCheckScope()) {
                EditorGUILayout.PropertyField(marginProp);
                EditorGUILayout.PropertyField(sizeProp);
                EditorGUILayout.PropertyField(padProp);
                EditorGUILayout.PropertyField(multProp);
                EditorGUILayout.PropertyField(controlProp);
                EditorGUILayout.PropertyField(overProp);
                EditorGUILayout.PropertyField(downProp);

                if (check.changed) {
                    serializedObject.ApplyModifiedProperties();
                    SceneView.RepaintAll();
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        void Axis(string label, ref float v0, ref float v1) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(20));
            v0 = EditorGUILayout.FloatField(GUIContent.none, v0);
            v1 = EditorGUILayout.FloatField(GUIContent.none, v1);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void OnSceneGUI() {
        HandleSlices();
        HandleScales();
        HandleMouseInput();
    }

    private void HandleSlices() {
        if (mode != EditorMode.Slice) return;
        if (axisX) {
            DrawSliceHandle(controlIds[0], GetSliceControlPosOf(0), GetVectorOf(0));
            DrawSliceHandle(controlIds[1], GetSliceControlPosOf(1), GetVectorOf(1));
        }
        if (axisY) {
            DrawSliceHandle(controlIds[2], GetSliceControlPosOf(2), GetVectorOf(2));
            DrawSliceHandle(controlIds[3], GetSliceControlPosOf(3), GetVectorOf(3));
        }
        if (axisZ) {
            DrawSliceHandle(controlIds[4], GetSliceControlPosOf(4), GetVectorOf(4));
            DrawSliceHandle(controlIds[5], GetSliceControlPosOf(5), GetVectorOf(5));
        }

        if (isDragging) {
            HandleSliceMouseDrag();
        }

        void DrawSliceHandle(int id, Vector3 pos, Vector3Int dir) {
            var ty = Event.current.type;
            var nearest = HandleUtility.nearestControl;

            var selected = nearest == id;
            if (!selected) Handles.color = slice.controlColor;
            else if (!isDragging) Handles.color = slice.controlOverColor;
            else Handles.color = slice.controlDownColor;

            var bounds = GetControlBounds();
            var size = Vector3.Scale(bounds.size, dir);
            var rot = Quaternion.LookRotation(dir);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            if (ty == EventType.Layout) {
                HandleUtility.AddControl(id, HandleUtility.DistanceToRectangle(pos, rot, Mathf.Max(size.x, size.y, size.z)));
            } else if (ty == EventType.Repaint) {
                GetRectangleVerts(pos, dir, bounds.size * slice.slicePanelMultiplier + Vector3.one * slice.slicePanelPadding, ref rectCache);
                Handles.DrawSolidRectangleWithOutline(rectCache, Handles.color, Handles.color);
            }
        }
    }

    private void GetRectangleVerts(Vector3 pos, Vector3 axis, Vector3 size, ref Vector3[] verts) {
        verts[0] = verts[1] = verts[2] = verts[3] = pos;

        if (axis == Vector3.up || axis == Vector3.down) {
            verts[0] += new Vector3(-size.x, 0, -size.z);
            verts[1] += new Vector3(size.x, 0, -size.z);
            verts[2] += new Vector3(size.x, 0, size.z);
            verts[3] += new Vector3(-size.x, 0, size.z);
        }
        if (axis == Vector3.left || axis == Vector3.right) {
            verts[0] += new Vector3(0, -size.y, -size.z);
            verts[1] += new Vector3(0, size.y, -size.z);
            verts[2] += new Vector3(0, size.y, size.z);
            verts[3] += new Vector3(0, -size.y, size.z);
        }
        if (axis == Vector3.forward || axis == Vector3.back) {
            verts[0] += new Vector3(-size.x, -size.y, 0);
            verts[1] += new Vector3(size.x, -size.y, 0);
            verts[2] += new Vector3(size.x, size.y, 0);
            verts[3] += new Vector3(-size.x, size.y, 0);
        }
    }

    private void HandleScales() {
        if (mode != EditorMode.Scale) return;
        if (axisX) {
            DrawScaleHandle(controlIds[0], GetScaleControlPosOf(0));
            DrawScaleHandle(controlIds[1], GetScaleControlPosOf(1));
        }
        if (axisY) {
            DrawScaleHandle(controlIds[2], GetScaleControlPosOf(2));
            DrawScaleHandle(controlIds[3], GetScaleControlPosOf(3));
        }
        if (axisZ) {
            DrawScaleHandle(controlIds[4], GetScaleControlPosOf(4));
            DrawScaleHandle(controlIds[5], GetScaleControlPosOf(5));
        }

        if (isDragging) {
            HandleScaleMouseDrag();
        }

        void DrawScaleHandle(int id, Vector3 pos) {
            var ty = Event.current.type;
            var nearest = HandleUtility.nearestControl;
            var size = slice.controlSize;

            var selected = nearest == id;
            if (!selected) Handles.color = slice.controlColor;
            else if (!isDragging) Handles.color = slice.controlOverColor;
            else Handles.color = slice.controlDownColor;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.SphereHandleCap(id, pos, Quaternion.identity, size, ty);
        }
    }

    private void HandleMouseInput() {
        var e = Event.current;
        var nearest = HandleUtility.nearestControl;

        if (e.alt || e.command || e.control || e.shift) return;
        if (e.type == EventType.MouseDown && e.button == 0) {
            if (mode == EditorMode.Slice) {
                TryInitDown(0, GetVectorOf(0), slice.x0);
                TryInitDown(1, GetVectorOf(1), slice.x1);
                TryInitDown(2, GetVectorOf(2), slice.y0);
                TryInitDown(3, GetVectorOf(3), slice.y1);
                TryInitDown(4, GetVectorOf(4), slice.z0);
                TryInitDown(5, GetVectorOf(5), slice.z1);
            } else {
                TryInitDown(0, GetVectorOf(0), slice.x2);
                TryInitDown(1, GetVectorOf(1), slice.x3);
                TryInitDown(2, GetVectorOf(2), slice.y2);
                TryInitDown(3, GetVectorOf(3), slice.y3);
                TryInitDown(4, GetVectorOf(4), slice.z2);
                TryInitDown(5, GetVectorOf(5), slice.z3);
            }
        }

        if (e.type == EventType.MouseUp && e.button == 0) {
            isDragging = false;
        }

        void TryInitDown(int index, Vector3Int mask, float init) {
            if (controlIds[index] == nearest) {
                var pos = GetScaleControlPosOf(index);
                var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                var plane = new Plane(GetNormalVectorOf(index), pos);
                if (plane.Raycast(ray, out var d)) {
                    var hit = ray.GetPoint(d);
                    var axisValue = Vector3.Dot(hit, mask);
                    downOffset = init - axisValue;
                }
                isDragging = true;
                draggingId = nearest;
            }
        }
    }

    private void HandleSliceMouseDrag() {
        TryDragControl(0, GetVectorOf(0), ref slice.x0, ref slice.x2);
        TryDragControl(1, GetVectorOf(1), ref slice.x1, ref slice.x3);
        TryDragControl(2, GetVectorOf(2), ref slice.y0, ref slice.y2);
        TryDragControl(3, GetVectorOf(3), ref slice.y1, ref slice.y3);
        TryDragControl(4, GetVectorOf(4), ref slice.z0, ref slice.z2);
        TryDragControl(5, GetVectorOf(5), ref slice.z1, ref slice.z3);

        bool TryDragControl(int index, Vector3Int mask, ref float result, ref float inverse) {
            if (draggingId == controlIds[index]) {
                var pos = GetScaleControlPosOf(index);
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var plane = new Plane(GetNormalVectorOf(index), pos);
                if (plane.Raycast(ray, out var d)) {
                    var hit = ray.GetPoint(d);
                    var axisValue = Vector3.Dot(hit, mask);
                    var newResult = axisValue + downOffset;
                    var delta = newResult - result;
                    result = newResult;
                    inverse += delta;
                    slice.Slice();
                    return true;
                }
            }
            return false;
        }
    }

    private void HandleScaleMouseDrag() {
        TryDragControl(0, GetVectorOf(0), ref slice.x2);
        TryDragControl(1, GetVectorOf(1), ref slice.x3);
        TryDragControl(2, GetVectorOf(2), ref slice.y2);
        TryDragControl(3, GetVectorOf(3), ref slice.y3);
        TryDragControl(4, GetVectorOf(4), ref slice.z2);
        TryDragControl(5, GetVectorOf(5), ref slice.z3);

        bool TryDragControl(int index, Vector3Int mask, ref float result) {
            if (draggingId == controlIds[index]) {
                var pos = GetScaleControlPosOf(index);
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var plane = new Plane(GetNormalVectorOf(index), pos);
                if (plane.Raycast(ray, out var d)) {
                    var hit = ray.GetPoint(d);
                    var axisValue = Vector3.Dot(hit, mask);
                    result = axisValue + downOffset;
                    slice.Slice();
                    return true;
                }
            }
            return false;
        }
    }

    private float GetSliceControlAxisPosOf(int index) {
        var margin = slice.controlMargin;

        if (index == 0) return slice.X(slice.x0) - margin;
        if (index == 1) return slice.X(slice.x1) + margin;
        if (index == 2) return slice.Y(slice.y0) - margin;
        if (index == 3) return slice.Y(slice.y1) + margin;
        if (index == 4) return slice.Z(slice.z0) - margin;
        if (index == 5) return slice.Z(slice.z1) + margin;
        throw new System.IndexOutOfRangeException("GetSliceControlAxisPosOf");
    }

    private float GetScaleControlAxisPosOf(int index) {
        var margin = slice.controlMargin;
        var min = slice.originalBounds.min;
        var max = slice.originalBounds.max;

        if (index == 0) return min.x + slice.x2 - slice.x0 - margin;
        if (index == 1) return max.x + slice.x3 - slice.x1 + margin;
        if (index == 2) return min.y + slice.y2 - slice.y0 - margin;
        if (index == 3) return max.y + slice.y3 - slice.y1 + margin;
        if (index == 4) return min.z + slice.z2 - slice.z0 - margin;
        if (index == 5) return max.z + slice.z3 - slice.z1 + margin;
        throw new System.IndexOutOfRangeException("GetScaleControlAxisPosOf");
    }

    private float GetBoundsAxisPosOf(int index) {
        var margin = slice.controlMargin;
        var min = slice.originalBounds.min;
        var max = slice.originalBounds.max;

        if (index == 0) return min.x + slice.x2 - slice.x0 - margin;
        if (index == 1) return max.x + slice.x3 - slice.x1 + margin;
        if (index == 2) return min.y + slice.y2 - slice.y0 - margin;
        if (index == 3) return max.y + slice.y3 - slice.y1 + margin;
        if (index == 4) return min.z + slice.z2 - slice.z0 - margin;
        if (index == 5) return max.z + slice.z3 - slice.z1 + margin;
        throw new System.IndexOutOfRangeException("GetBoundsAxisPosOf");
    }

    private Vector3 GetSliceControlPosOf(int index) {
        var center = GetControlBounds().center;
        if (index == 0) return center.WithX(GetSliceControlAxisPosOf(0));
        if (index == 1) return center.WithX(GetSliceControlAxisPosOf(1));
        if (index == 2) return center.WithY(GetSliceControlAxisPosOf(2));
        if (index == 3) return center.WithY(GetSliceControlAxisPosOf(3));
        if (index == 4) return center.WithZ(GetSliceControlAxisPosOf(4));
        if (index == 5) return center.WithZ(GetSliceControlAxisPosOf(5));
        throw new System.IndexOutOfRangeException("GetSliceControlPosOf");
    }

    private Vector3 GetScaleControlPosOf(int index) {
        var center = GetControlBounds().center;
        if (index == 0) return center.WithX(GetScaleControlAxisPosOf(0));
        if (index == 1) return center.WithX(GetScaleControlAxisPosOf(1));
        if (index == 2) return center.WithY(GetScaleControlAxisPosOf(2));
        if (index == 3) return center.WithY(GetScaleControlAxisPosOf(3));
        if (index == 4) return center.WithZ(GetScaleControlAxisPosOf(4));
        if (index == 5) return center.WithZ(GetScaleControlAxisPosOf(5));
        throw new System.IndexOutOfRangeException("GetScaleControlPosOf");
    }

    private Bounds GetControlBounds() {
        var x0 = GetBoundsAxisPosOf(0);
        var x1 = GetBoundsAxisPosOf(1);
        var y0 = GetBoundsAxisPosOf(2);
        var y1 = GetBoundsAxisPosOf(3);
        var z0 = GetBoundsAxisPosOf(4);
        var z1 = GetBoundsAxisPosOf(5);

        var center = new Vector3(x0 + x1, y0 + y1, z0 + z1) / 2;
        var size = new Vector3(x1 - x0, y1 - y0, z1 - z0) / 2;
        return new Bounds(center, size);
    }

    private Vector3Int GetVectorOf(int index) {
        if (index == 0) return Vector3Int.right;
        if (index == 1) return Vector3Int.right;
        if (index == 2) return Vector3Int.up;
        if (index == 3) return Vector3Int.up;
        if (index == 4) return new Vector3Int(0, 0, 1);
        if (index == 5) return new Vector3Int(0, 0, 1);
        throw new System.IndexOutOfRangeException("GetVectorOf");
    }

    private Vector3 GetNormalVectorOf(int index) {
        if (index == 0) return Vector3.up;
        if (index == 1) return Vector3.up;
        if (index == 2) return Vector3.forward;
        if (index == 3) return Vector3.forward;
        if (index == 4) return Vector3.right;
        if (index == 5) return Vector3.right;
        throw new System.IndexOutOfRangeException("GetNormalVectorOf");
    }

    void SetSliceDirty() {

    }
}