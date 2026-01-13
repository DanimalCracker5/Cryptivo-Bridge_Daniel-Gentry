using UnityEngine;
public enum PlacementStage { StartPiece, EndPiece, Null, BuildInProgress }
public class BridgeConstructor : MonoBehaviour
{
    #region Variables
    [Header("State")]
    public PlacementStage CurrentPlacementStage = PlacementStage.StartPiece;
    private PlacementStage _cachedStage = PlacementStage.Null;

    [Header("Rotation")]
    public float MouseRotateSensitivity = 180f;

    [Header("Prefabs")]
    public GameObject Bridge_Start_Prefab;
    public GameObject Bridge_Mid_Prefab;
    public GameObject Bridge_Mid_Extensive_Prefab;
    public GameObject Bridge_End_Prefab;

    private Transform _cursor, _startPiece, _endPiece, _midPreviewRoot;
    private Vector3 _lastHitPoint, _mouseDownStartPoint;
    private bool _dragActive;
    private float MinDragDistance = 0.6f;
    private float MinBridgeLength = 1.5f;

    private Vector3 _lastSpawnFwd, _lastSpawnEndPos, _lastSpawnEdge;
    private float _lastSpawnY;     
    #endregion
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            CancelPlacement();

        if (Physics.Raycast(transform.position, transform.forward, out var hit))
            _lastHitPoint = hit.point;

        //Placement Visuals
        UpdatePlacementVisual();
        UpdatePlacementVisualPositionAndRotation();

        //Dragging & Releasing
        HandleDragInput();
        HandleDragRelease();
    }
    #region Placement Visual
    private void UpdatePlacementVisual()
    {
        var prefab = GetCursorPrefabForStage(CurrentPlacementStage);
        if (!prefab) return;

        if (_cachedStage == CurrentPlacementStage && _cursor)
            return;

        _cachedStage = CurrentPlacementStage;

        var keepRotation = _cursor ? _cursor.rotation : Quaternion.identity;
        if (_cursor) Destroy(_cursor.gameObject);

        _cursor = Instantiate(prefab).transform;
        _cursor.rotation = keepRotation;
    }
    private void UpdatePlacementVisualPositionAndRotation()
    {
        if (!_cursor) return;

        _cursor.position = _lastHitPoint;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.00001f)
            _cursor.Rotate(0f, scroll * MouseRotateSensitivity, 0f, Space.World);

        if (!_dragActive)
            ClearMidPreview();
    }
    #endregion 
    #region Placement 
    private void FinalizeBridgePlacement()
    {
        float yLock = _startPiece.position.y;

        Vector3 endPos = _cursor.position;
        endPos.y = yLock;

        Vector3 flat = endPos - _startPiece.position;
        flat.y = 0f;

        if (flat.magnitude < MinBridgeLength) { CancelPlacement(); return; }
        if (!Bridge_End_Prefab) { CancelPlacement(); return; }

        _endPiece = Instantiate(Bridge_End_Prefab, endPos, _cursor.rotation).transform;

        ApplyEndpointRotations(_startPiece, _endPiece, _endPiece.position - _startPiece.position);

        ClearMidPreview();

        BuildMidPieces(
            parent: null,
            startPos: _startPiece.position,
            endPos: _endPiece.position,
            yLock: yLock
        );

        SnapEndPieceToLast(yLock);

        _startPiece = null;
        _endPiece = null;
        CurrentPlacementStage = PlacementStage.StartPiece;
    }
    private void BuildMidPieces(Transform parent, Vector3 startPos, Vector3 endPos, float yLock)
    {
        Vector3 dir = endPos - startPos;
        dir.y = 0f;

        float total = dir.magnitude;
        if (total < MinBridgeLength) return;

        Vector3 fwd = dir / Mathf.Max(total, 0.0001f);
        Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);

        float startLen = GetPrefabLengthAlongXAxis(Bridge_Start_Prefab);
        float endLen = GetPrefabLengthAlongXAxis(Bridge_End_Prefab);

        Vector3 startInner = startPos + fwd * (startLen * 0.5f);
        Vector3 endInner = endPos - fwd * (endLen * 0.5f);
        startInner.y = yLock;
        endInner.y = yLock;

        Vector3 span = endInner - startInner;
        span.y = 0f;

        float remaining = span.magnitude;

        float extLen = GetPrefabLengthAlongXAxis(Bridge_Mid_Extensive_Prefab);
        float midLen = GetPrefabLengthAlongXAxis(Bridge_Mid_Prefab);

        if (remaining < MinBridgeLength) return;
        if (extLen <= 0.0001f || midLen <= 0.0001f) return;

        Vector3 edge = startInner;
        _lastSpawnEdge = startInner;
        _lastSpawnFwd = fwd;
        _lastSpawnY = yLock;
        _lastSpawnEndPos = endPos;

        if (remaining >= extLen)
        {
            Spawn(parent, Bridge_Mid_Extensive_Prefab, edge, fwd, rot, extLen, yLock);
            edge += fwd * extLen;
            remaining -= extLen;
        }

        while (remaining >= (midLen + extLen))
        {
            Spawn(parent, Bridge_Mid_Prefab, edge, fwd, rot, midLen, yLock);
            edge += fwd * midLen;
            remaining -= midLen;

            Spawn(parent, Bridge_Mid_Extensive_Prefab, edge, fwd, rot, extLen, yLock);
            edge += fwd * extLen;
            remaining -= extLen;
        }

        while (remaining >= extLen)
        {
            Spawn(parent, Bridge_Mid_Extensive_Prefab, edge, fwd, rot, extLen, yLock);
            edge += fwd * extLen;
            remaining -= extLen;
        }

        _lastSpawnEdge = edge;
    }
    private void SnapEndPieceToLast(float yLock)
    {
        if (!_endPiece) return;

        float endPieceLen = GetPrefabLengthAlongXAxis(Bridge_End_Prefab);
        Vector3 snapped = _lastSpawnEdge + _lastSpawnFwd * (endPieceLen * 0.5f);
        snapped.y = yLock;

        _endPiece.position = snapped;
    }
    void CancelPlacement()
    {
        ClearMidPreview();

        DestroyGameObjectFor(_startPiece);
        DestroyGameObjectFor(_endPiece);

        _startPiece = null;
        _endPiece = null;
        _dragActive = false;

        CurrentPlacementStage = PlacementStage.StartPiece;
    }
    #endregion
    #region Drag Input  
    private void HandleDragInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _mouseDownStartPoint = _lastHitPoint;
            _dragActive = false;
        }

        if (!Input.GetMouseButton(1))
            return;

        TryBeginDrag();
        UpdateDragPreviewIfActive();
    }
    private void TryBeginDrag()
    {
        if (_dragActive) return;
        if (CurrentPlacementStage != PlacementStage.StartPiece) return;

        if (Vector3.Distance(_mouseDownStartPoint, _lastHitPoint) < MinDragDistance)
            return;

        _dragActive = true;

        DestroyGameObjectFor(_startPiece);
        DestroyGameObjectFor(_endPiece);
        ClearMidPreview();

        if (!_cursor || !Bridge_Start_Prefab) return;

        _startPiece = Instantiate(Bridge_Start_Prefab, _cursor.position, _cursor.rotation).transform;
        CurrentPlacementStage = PlacementStage.EndPiece;
    }
    private void UpdateDragPreviewIfActive()
    {
        if (!_dragActive) return;
        if (CurrentPlacementStage != PlacementStage.EndPiece) return;
        if (!_startPiece || !_cursor) return;

        float yLock = _startPiece.position.y;
        LockCursorY(yLock);

        if (Vector3.Distance(_startPiece.position, _cursor.position) < MinBridgeLength)
        {
            ClearMidPreview();
            return;
        }

        Vector3 flatDir = _cursor.position - _startPiece.position;
        flatDir.y = 0f;

        ApplyEndpointRotations(_startPiece, _cursor, flatDir);

        EnsureMidPreviewRoot();
        ClearMidPreviewChildren();

        BuildMidPieces(
            parent: _midPreviewRoot,
            startPos: _startPiece.position,
            endPos: _cursor.position,
            yLock: yLock
        );
    }
    private void HandleDragRelease()
    {
        if (!Input.GetMouseButtonUp(1))
            return;

        if (_dragActive && CurrentPlacementStage == PlacementStage.EndPiece && _startPiece && _cursor)
            FinalizeBridgePlacement();

        _dragActive = false;
    }
    #endregion
    #region Helpers
    private GameObject GetCursorPrefabForStage(PlacementStage stage)
    {
        return stage == PlacementStage.StartPiece ? Bridge_Start_Prefab : Bridge_End_Prefab;
    }
    private void LockCursorY(float yLock)
    {
        var p = _cursor.position;
        p.y = yLock;
        _cursor.position = p;
    }
    private static void ApplyEndpointRotations(Transform start, Transform end, Vector3 direction)
    {
        if (!start || !end) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.000001f) return;

        start.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
        end.rotation = Quaternion.LookRotation(-direction, Vector3.up) * Quaternion.Euler(0f, 270f, 0f);
    }
    private void EnsureMidPreviewRoot()
    {
        if (_midPreviewRoot) return;
        _midPreviewRoot = new GameObject("BridgeMidPreview").transform;
    }
    private void ClearMidPreviewChildren()
    {
        if (!_midPreviewRoot) return;
        for (int i = _midPreviewRoot.childCount - 1; i >= 0; i--)
            Destroy(_midPreviewRoot.GetChild(i).gameObject);
    }
    private void ClearMidPreview()
    {
        if (_midPreviewRoot)
            Destroy(_midPreviewRoot.gameObject);
        _midPreviewRoot = null;
    }
    private static void DestroyGameObjectFor(Transform _transform)
    {
        if (_transform) 
            Object.Destroy(_transform.gameObject);
    }
    static void Spawn(Transform parent, GameObject prefab, Vector3 edge, Vector3 fwd, Quaternion rot, float len, float yLock)
    {
        if (!prefab) return;

        Vector3 c = edge + fwd * (len * 0.5f);
        c.y = yLock;

        if (parent) Object.Instantiate(prefab, c, rot, parent);
        else Object.Instantiate(prefab, c, rot);
    }

    float GetPrefabLengthAlongXAxis(GameObject prefab)
    {
        var r = prefab.GetComponentInChildren<Renderer>();
        return r.bounds.size.x;
    }
    #endregion
}
