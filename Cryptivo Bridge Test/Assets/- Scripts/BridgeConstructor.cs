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

    private const float BRIDGE_YAW_OFFSET = 90f;
    private const float END_EXTRA_YAW = 180f;
    #endregion

    #region Unity Methods
    private void Update()
    {
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
            FaceEachOther(_startPiece, cursor);
            VisualizeFillBetween(_startPiece, cursor);
        }
        else
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            cursor.Rotate(0f, scroll * MouseRotateSensitivity, 0f, Space.World);
            ClearMidPreview();
        }
    }

    void SetCursorVisual(GameObject gameObject)
    {
        if (CurrentPlacementStage == _cachedPlacementStage) return;

        _cachedPlacementStage = CurrentPlacementStage;

        Quaternion cachedRotation = cursor ? cursor.rotation : Quaternion.identity;

        if (cursor) Destroy(cursor.gameObject);
        cursor = Instantiate(gameObject).transform;

        cursor.rotation = cachedRotation;
    }
    #endregion

    #region Placing & Building
    void Place()
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
            _endPiece = Instantiate(Bridge_End_Prefab, cursor.position, cursor.rotation).transform;

            FaceEachOther(_startPiece, _endPiece);
            FillBetween(_startPiece, _endPiece);

            BuildBridge();
        }
    }

    void BuildBridge()
    {
        ClearMidPreview();
        _startPiece = null;
        _endPiece = null;
        CurrentPlacementStage = PlacementStage.StartPiece;
    }

    Quaternion LookBridge(Vector3 dir, float extraYaw = 0f)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return Quaternion.identity;
        return Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, BRIDGE_YAW_OFFSET + extraYaw, 0f);
    }

    void FaceEachOther(Transform a, Transform b)
    {
        if (a == null || b == null) return;

        Vector3 ab = b.position - a.position;

        a.rotation = LookBridge(ab);
        b.rotation = LookBridge(-ab, END_EXTRA_YAW);
    }

    float PieceLen(GameObject prefab)
    {
        return prefab.GetComponentInChildren<Renderer>().bounds.size.z;
    }

    void FillBetween(Transform a, Transform b)
    {
        Vector3 d = b.position - a.position; d.y = 0f;
        float dist = d.magnitude;
        if (dist < 0.0001f) return;

        Vector3 dir = d / dist;
        Quaternion rot = LookBridge(dir);

        float midStep = PieceLen(Bridge_Mid_Prefab);
        float fillStep = PieceLen(Bridge_Mid_Extensive_Prefab);

        float offset = midStep;

        // Place as many middle sections as possible
        while (offset + midStep < dist)
        {
            Instantiate(Bridge_Mid_Prefab, a.position + dir * offset, rot);
            offset += midStep;
        }

        // Fill remaining gap with filler/extensive sections
        while (offset + fillStep < dist)
        {
            Instantiate(Bridge_Mid_Extensive_Prefab, a.position + dir * offset, rot);
            offset += fillStep;
        }
    }

    void VisualizeFillBetween(Transform a, Transform b)
    {
        if (_midPreviewRoot == null)
            _midPreviewRoot = new GameObject("BridgeMidPreview").transform;

        for (int i = _midPreviewRoot.childCount - 1; i >= 0; i--)
            Destroy(_midPreviewRoot.GetChild(i).gameObject);

        Vector3 d = b.position - a.position; d.y = 0f;
        float dist = d.magnitude;
        if (dist < 0.0001f) return;

        Vector3 dir = d / dist;
        Quaternion rot = LookBridge(dir);

        float midStep = PieceLen(Bridge_Mid_Prefab);
        float fillStep = PieceLen(Bridge_Mid_Extensive_Prefab);

        float offset = midStep;

        while (offset + midStep < dist)
        {
            Instantiate(Bridge_Mid_Prefab, a.position + dir * offset, rot, _midPreviewRoot);
            offset += midStep;
        }

        while (offset + fillStep < dist)
        {
            Instantiate(Bridge_Mid_Extensive_Prefab, a.position + dir * offset, rot, _midPreviewRoot);
            offset += fillStep;
        }
    }

    void ClearMidPreview()
    {
        if (_midPreviewRoot) Destroy(_midPreviewRoot.gameObject);
        _midPreviewRoot = null;
    }
    #endregion
}
