using UnityEngine;
public enum PlacementStage { StartPiece, EndPiece, _BuildInProgress, Null}
public class BridgeConstructor : MonoBehaviour
{
    #region Variables
    public PlacementStage CurrentPlacementStage = PlacementStage.StartPiece;
    private PlacementStage cachedPlacementStage = PlacementStage.Null;

    Vector3 lastHitPoint;

    [Header("Prefabs")] 
    public GameObject 
        Bridge_Start_Prefab,
        Bridge_Mid_Prefab,
        Bridge_Mid_Extensive_Prefab,
        Bridge_End_Prefab;

    Transform cursor;
    #endregion
    private void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, out var hit))
            lastHitPoint = hit.point;

        UpdateCursor();
    }
    private void UpdateCursor()
    {
        //Create placement visual
        if (CurrentPlacementStage == PlacementStage.StartPiece)
            SetCursorVisual(Bridge_Start_Prefab); 
        
        else if(CurrentPlacementStage == PlacementStage.EndPiece)
            SetCursorVisual(Bridge_End_Prefab);

        //Update cursor position
        if (cursor != null)
            cursor.position = lastHitPoint;
    }
    void SetCursorVisual(GameObject gameObject)
    {
        bool PlacementModeHasChanged = (CurrentPlacementStage != cachedPlacementStage);

        if (PlacementModeHasChanged)
        {
            cachedPlacementStage = CurrentPlacementStage;

            //Only change our cursor prefab when placing mode changes
            if (cursor != null) Destroy(cursor);
            cursor = Instantiate(gameObject).transform;
        }
    }
}
