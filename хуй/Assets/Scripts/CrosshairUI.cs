using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;
    public LayerMask enemyDetectionMask = ~0;
    public float enemyDetectDistance = 120f;

    [Header("Crosshair Elements")]
    public RectTransform lineTop;
    public RectTransform lineBottom;
    public RectTransform lineLeft;
    public RectTransform lineRight;
    public RectTransform centerDot;

    [Header("Images (for color tinting)")]
    public Image[] crosshairImages;

    [Header("Profiles")]
    public CrosshairProfile knifeProfile;
    public CrosshairProfile fastProfile;
    public CrosshairProfile heavyProfile;
    public CrosshairProfile rokletProfile;

    [Header("Tuning")]
    public float spreadLerpSpeed = 12f;
    public float colorLerpSpeed = 15f;

    private float currentSpread;
    private float shootExpansion;
    private CrosshairProfile activeProfile;
    private bool isADS;
    private float moveSpeedNormalized;
    private Color currentColor;

    private void Start()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        activeProfile = knifeProfile;
        ApplyProfileVisuals();
        currentColor = activeProfile != null ? activeProfile.normalColor : Color.white;
    }

    private void LateUpdate()
    {
        if (activeProfile == null) return;

        // Decay shoot expansion
        shootExpansion = Mathf.MoveTowards(shootExpansion, 0f, activeProfile.contractSpeed * Time.deltaTime);

        // Calculate target spread
        float targetSpread = activeProfile.baseSpread
            + shootExpansion
            + activeProfile.moveExpandAmount * moveSpeedNormalized;

        if (isADS)
            targetSpread *= activeProfile.adsSpreadMultiplier;

        targetSpread = Mathf.Clamp(targetSpread, 0f, activeProfile.maxSpread);

        // Lerp spread
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * spreadLerpSpeed);

        // Enemy hover detection
        Color targetColor = activeProfile.normalColor;
        if (aimCamera != null)
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, enemyDetectDistance, enemyDetectionMask, QueryTriggerInteraction.Ignore))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead)
                    targetColor = activeProfile.enemyHoverColor;
            }
        }

        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);

        // Apply positions
        ApplyLayout();
        ApplyColor();
    }

    private void ApplyLayout()
    {
        float halfLine = activeProfile.lineLength * 0.5f;

        if (lineTop != null)
            lineTop.anchoredPosition = new Vector2(0f, currentSpread + halfLine);

        if (lineBottom != null)
            lineBottom.anchoredPosition = new Vector2(0f, -(currentSpread + halfLine));

        if (lineLeft != null)
            lineLeft.anchoredPosition = new Vector2(-(currentSpread + halfLine), 0f);

        if (lineRight != null)
            lineRight.anchoredPosition = new Vector2(currentSpread + halfLine, 0f);
    }

    private void ApplyColor()
    {
        if (crosshairImages == null) return;

        for (int i = 0; i < crosshairImages.Length; i++)
        {
            if (crosshairImages[i] != null)
                crosshairImages[i].color = currentColor;
        }
    }

    private void ApplyProfileVisuals()
    {
        if (activeProfile == null) return;

        // Line sizes
        if (lineTop != null)
            lineTop.sizeDelta = new Vector2(activeProfile.lineThickness, activeProfile.lineLength);

        if (lineBottom != null)
            lineBottom.sizeDelta = new Vector2(activeProfile.lineThickness, activeProfile.lineLength);

        if (lineLeft != null)
            lineLeft.sizeDelta = new Vector2(activeProfile.lineLength, activeProfile.lineThickness);

        if (lineRight != null)
            lineRight.sizeDelta = new Vector2(activeProfile.lineLength, activeProfile.lineThickness);

        // Center dot
        if (centerDot != null)
        {
            centerDot.gameObject.SetActive(activeProfile.showCenterDot);
            centerDot.sizeDelta = new Vector2(activeProfile.dotSize, activeProfile.dotSize);
        }
    }

    // --- Public API ---

    public void OnShot()
    {
        if (activeProfile == null) return;
        shootExpansion += activeProfile.shootExpandAmount;
        shootExpansion = Mathf.Min(shootExpansion, activeProfile.maxSpread);
    }

    public void OnWeaponChanged(WeaponType? type)
    {
        activeProfile = GetProfileForWeapon(type);
        ApplyProfileVisuals();
        shootExpansion = 0f;
    }

    public void SetMoveSpeed(float normalized01)
    {
        moveSpeedNormalized = Mathf.Clamp01(normalized01);
    }

    public void SetADS(bool ads)
    {
        isADS = ads;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private CrosshairProfile GetProfileForWeapon(WeaponType? type)
    {
        if (type == null) return knifeProfile;

        switch (type.Value)
        {
            case WeaponType.Fast: return fastProfile;
            case WeaponType.Heavy: return heavyProfile;
            case WeaponType.Roklet: return rokletProfile;
            default: return knifeProfile;
        }
    }
}
