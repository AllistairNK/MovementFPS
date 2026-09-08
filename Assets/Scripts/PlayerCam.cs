using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    [Tooltip("Caps how many degrees the camera can turn in a single frame. Guards against a frame-time stutter (e.g. from a physics glitch) turning a normal mouse delta into a large, sudden rotation snap.")]
    public float maxDegreesPerFrame = 15f;

    public Transform orientation;

    float xRotation;
    float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Mathf.Clamp(Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX, -maxDegreesPerFrame, maxDegreesPerFrame);
        float mouseY = Mathf.Clamp(Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY, -maxDegreesPerFrame, maxDegreesPerFrame);

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //rotate cam and orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
