using System;
using UnityEngine;
public class CameraController : MonoBehaviour
{
    #region Variables
    Camera _camera;
    float pitch, yaw;

    [Range(0.1f, 5f)]
    public float RotationSensitivity = 2f;

    [Range(5f, 10f)]
    public float MovementSensitivity = 6f;
    #endregion 
    void Awake()
    {
        _camera = GetComponent<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    } 
    private void Update()
    {
        UpdateRotation();
        UpdateMovement();
    }
    private void UpdateMovement()
    {
        //Get Inputs
        float _horizontal = (Input.GetAxis("Horizontal") * MovementSensitivity);
        float _vertical = (Input.GetAxis("Vertical") * MovementSensitivity);

        //Translate By Inputs
        transform.Translate(
            new Vector3(
                _horizontal * Time.deltaTime,
                0f,
                _vertical * Time.deltaTime)
            );
    }
    private void UpdateRotation()
    {
        yaw += Input.GetAxis("Mouse X") * RotationSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * RotationSensitivity;
        _camera.transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
    }
}
