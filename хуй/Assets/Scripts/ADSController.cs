using UnityEngine;

public class ADSController : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;
    public CrosshairUI crosshairUI;
    public MobileShoot mobileShoot;
    public MobileCameraSwipe cameraSwipe;

    [Header("FOV")]
    public float normalFOV = 60f;
    public float adsFOV = 40f;
    public float fovLerpSpeed = 10f;

    [Header("Sensitivity")]
    public float normalSensitivity = 0.12f;
    public float adsSensitivity = 0.06f;

    private bool isADS;
    private float targetFOV;

    private void Start()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        targetFOV = normalFOV;

        if (aimCamera != null)
            normalFOV = aimCamera.fieldOfView;
    }

    private void Update()
    {
        if (aimCamera == null) return;

        aimCamera.fieldOfView = Mathf.Lerp(aimCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
    }

    public void ToggleADS()
    {
        SetADS(!isADS);
    }

    public void SetADS(bool ads)
    {
        isADS = ads;
        targetFOV = isADS ? adsFOV : normalFOV;

        if (cameraSwipe != null)
            cameraSwipe.swipeSensitivity = isADS ? adsSensitivity : normalSensitivity;

        if (crosshairUI != null)
            crosshairUI.SetADS(isADS);
    }

    public void DisableADS()
    {
        if (isADS)
            SetADS(false);
    }
}
