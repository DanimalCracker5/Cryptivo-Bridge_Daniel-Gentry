using UnityEngine;

public enum PlacementStage { StartPiece, EndPiece, Null, BuildInProgress }

public class BridgeConstructor : MonoBehaviour
{
    #region Variables
    private Vector3 lastHitPoint;

    private Transform cursor,
        _startPiece,
        _endPiece,
        _midPreviewRoot;

    public PlacementStage CurrentPlacementStage = PlacementStage.StartPiece;
    private PlacementStage _cachedPlacementStage = PlacementStage.Null;

    [Header("Prefabs")]
    public GameObject
        Bridge_Start_Prefab,
        Bridge_Mid_Prefab,
        Bridge_Mid_Extensive_Prefab,
        Bridge_End_Prefab;

    [Header("Rotation")]
    public float MouseRotateSensitivity = 180f;
    #endregion 
    #region Unity Methods
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            CancelBuild();

        if (Physics.Raycast(transform.position, transform.forward, out var hit))
            lastHitPoint = hit.point;

        UpdateCursor();

        if (Input.GetMouseButtonDown(1))
            Place();
    }
    #endregion 
    #region Cursor
    private void UpdateCursor()
    {
        if (CurrentPlacementStage == PlacementStage.StartPiece)
            SetCursorVisual(Bridge_Start_Prefab);
        else if (CurrentPlacementStage == PlacementStage.EndPiece)
            SetCursorVisual(Bridge_End_Prefab);

        if (cursor != null)
            cursor.position = lastHitPoint;

        if (cursor == null) return;

        if (CurrentPlacementStage == PlacementStage.EndPiece && _startPiece != null)
        {
            Vector3 p = cursor.position;
            p.y = _startPiece.position.y;
            cursor.position = p;

            FaceEachOther(_startPiece, cursor);
            PreviewFillBetween(_startPiece, cursor);
        }
        else
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            cursor.Rotate(0f, scroll * MouseRotateSensitivity, 0f, Space.World);
            ClearMidPreview();
        }
    }
    private void SetCursorVisual(GameObject prefab)
    {
        if (CurrentPlacementStage == _cachedPlacementStage) return;

        _cachedPlacementStage = CurrentPlacementStage;

        Quaternion cachedRotation = cursor ? cursor.rotation : Quaternion.identity;

        if (cursor) Destroy(cursor.gameObject);
        cursor = Instantiate(prefab).transform;

        cursor.rotation = cachedRotation;
    }
    #endregion 
    #region Placing & Building
    private void Place()
    {
        if (cursor == null) return;

        if (CurrentPlacementStage == PlacementStage.StartPiece)
        {
            if (_startPiece) Destroy(_startPiece.gameObject);
            if (_endPiece) Destroy(_endPiece.gameObject);
            ClearMidPreview();

            _startPiece = Instantiate(Bridge_Start_Prefab, cursor.position, cursor.rotation).transform;
            CurrentPlacementStage = PlacementStage.EndPiece;
            return;
        }

        if (CurrentPlacementStage == PlacementStage.EndPiece)
        {
            Vector3 endPos = cursor.position;
            endPos.y = _startPiece.position.y;

            _endPiece = Instantiate(Bridge_End_Prefab, endPos, cursor.rotation).transform;

            FaceEachOther(_startPiece, _endPiece);
            PlaceFillBetween(_startPiece, _endPiece);

            BuildBridge();
        }
    }

    private void BuildBridge()
    {
        ClearMidPreview();
        _startPiece = null;
        _endPiece = null;
        CurrentPlacementStage = PlacementStage.StartPiece;
    }

    private void CancelBuild()
    {
        ClearMidPreview();
        if (_startPiece) Destroy(_startPiece.gameObject);
        if (_endPiece) Destroy(_endPiece.gameObject);
        _startPiece = null;
        _endPiece = null;
        CurrentPlacementStage = PlacementStage.StartPiece;
    }
    #endregion
    #region Filling
    private void PlaceFillBetween(Transform start, Transform end)
    {
        FillBetweenInternal(start, end, null);
    }
    private void PreviewFillBetween(Transform start, Transform end)
    {
        if (_midPreviewRoot == null)
            _midPreviewRoot = new GameObject("BridgeMidPreview").transform;

        for (int i = _midPreviewRoot.childCount - 1; i >= 0; i--)
            Destroy(_midPreviewRoot.GetChild(i).gameObject);

        FillBetweenInternal(start, end, _midPreviewRoot);
    }
    private void FillBetweenInternal(Transform start, Transform end, Transform parent)
    {
        Vector3 direction = end.position - start.position; direction.y = 0f;
        float distance = direction.magnitude;
        if (distance < 0.0001f) return;

        Vector3 unitDir = direction / distance;
        Quaternion rot = GetBridgeRotationFromDirection(unitDir);

        float startLength = GetPieceLengthFromInstance(start);
        float endLength = GetPieceLengthFromInstance(end);

        // Keep your existing behavior: different edge offset in preview vs final.
        float edgeOffset = parent == null ? +-1.25f : -1.25f;

        Vector3 startEdge = start.position + unitDir * (startLength * 0.5f + edgeOffset);
        Vector3 endEdge = end.position - unitDir * (endLength * 0.5f + edgeOffset);

        Vector3 span = endEdge - startEdge; span.y = 0f;
        float fillDistance = span.magnitude;
        if (fillDistance < 0.0001f) return;

        Vector3 fillDir = span / fillDistance;

        float segmentLength = GetPieceLengthFromPrefab(Bridge_Mid_Extensive_Prefab);
        float segmentSpacing = segmentLength * 0.50f;

        float pos = segmentLength * 0.5f;
        while (pos + segmentLength * 0.5f <= fillDistance)
        {
            if (parent == null) Instantiate(Bridge_Mid_Extensive_Prefab, startEdge + fillDir * pos, rot);
            else Instantiate(Bridge_Mid_Extensive_Prefab, startEdge + fillDir * pos, rot, parent);
            pos += segmentSpacing;
        }
    }
    #endregion
    #region Helpers
    private Quaternion GetBridgeRotationFromDirection(Vector3 direction, float extraYawDegrees = 0f)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return Quaternion.identity;
        return Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f + extraYawDegrees, 0f);
    }
    private void FaceEachOther(Transform a, Transform b)
    {
        if (a == null || b == null) return;

        Vector3 ab = b.position - a.position;
        a.rotation = GetBridgeRotationFromDirection(ab);
        b.rotation = GetBridgeRotationFromDirection(-ab, 180f);
    }
    private float GetPieceLengthFromPrefab(GameObject prefab)
    {
        var mf = prefab.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Vector3 meshSize = mf.sharedMesh.bounds.size;
            Vector3 lossy = mf.transform.lossyScale;
            float lengthX = Mathf.Abs(meshSize.x * lossy.x);
            float lengthZ = Mathf.Abs(meshSize.z * lossy.z);
            return Mathf.Max(lengthX, lengthZ);
        }

        var r = prefab.GetComponentInChildren<Renderer>();
        return Mathf.Max(r.bounds.size.x, r.bounds.size.z);
    }
    private float GetPieceLengthFromInstance(Transform instance)
    {
        var mf = instance.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Vector3 meshSize = mf.sharedMesh.bounds.size;
            Vector3 lossy = mf.transform.lossyScale;
            float lengthX = Mathf.Abs(meshSize.x * lossy.x);
            float lengthZ = Mathf.Abs(meshSize.z * lossy.z);
            return Mathf.Max(lengthX, lengthZ);
        }

        var r = instance.GetComponentInChildren<Renderer>();
        return Mathf.Max(r.bounds.size.x, r.bounds.size.z);
    }
    private void ClearMidPreview()
    {
        if (_midPreviewRoot) Destroy(_midPreviewRoot.gameObject);
        _midPreviewRoot = null;
    }
    #endregion
}
