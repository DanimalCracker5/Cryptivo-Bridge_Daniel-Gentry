using UnityEngine;
public enum PlacementStage { StartPiece, EndPiece, Null, BuildInProgress }
public class BridgeConstructor : MonoBehaviour
{
    #region Variables
    private Vector3 lastHitPoint;

    public PlacementStage CurrentPlacementStage = PlacementStage.StartPiece;
    private PlacementStage _cachedPlacementStage = PlacementStage.Null;

    [Header("Prefabs")]
    public GameObject
        Bridge_Start_Prefab,
        Bridge_Mid_Prefab,
        Bridge_Mid_Extensive_Prefab,
        Bridge_End_Prefab;

    Transform cursor;

    [Header("Rotation")]
    public float MouseRotateSensitivity = 180f; // degrees per second 

    private Transform _startPiece;
    private Transform _endPiece;
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
        //Create placement visual
        if (CurrentPlacementStage == PlacementStage.StartPiece)
            SetCursorVisual(Bridge_Start_Prefab);

        else if (CurrentPlacementStage == PlacementStage.EndPiece)
            SetCursorVisual(Bridge_End_Prefab);

        //Update cursor position
        if (cursor != null)
            cursor.position = lastHitPoint;

        //Update cursor rotation
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (cursor != null)
            cursor.Rotate(0f, scroll * MouseRotateSensitivity, 0f);
    }
    void SetCursorVisual(GameObject gameObject)
    {
        bool PlacementModeHasChanged = (CurrentPlacementStage != _cachedPlacementStage);

        // Only change our cursor prefab when placing mode changes
        if (PlacementModeHasChanged)
        {
            _cachedPlacementStage = CurrentPlacementStage;
            Quaternion cachedRotation = cursor != null ? cursor.rotation : Quaternion.identity;
            if (cursor != null) Destroy(cursor.gameObject);
            cursor = Instantiate(gameObject).transform;
            cursor.rotation = cachedRotation;
        }
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

            _startPiece = Instantiate(Bridge_Start_Prefab, cursor.position, cursor.rotation).transform;
            CurrentPlacementStage = PlacementStage.EndPiece;
            return;
        }

        else if (CurrentPlacementStage == PlacementStage.EndPiece)
        {
            _endPiece = Instantiate(Bridge_End_Prefab, cursor.position, cursor.rotation).transform;
            CurrentPlacementStage = PlacementStage.BuildInProgress;
        }
    }
    void BuildBridge()
    {
        _startPiece = null;
        _endPiece = null;
        CurrentPlacementStage = PlacementStage.StartPiece;
    }
    #endregion
}
