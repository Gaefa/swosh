using UnityEngine;

[CreateAssetMenu(fileName = "CrosshairProfile", menuName = "Weapons/Crosshair Profile")]
public class CrosshairProfile : ScriptableObject
{
    [Header("Spread (pixels)")]
    public float baseSpread = 10f;
    public float maxSpread = 55f;
    public float shootExpandAmount = 6f;
    public float moveExpandAmount = 12f;
    public float contractSpeed = 90f;

    [Header("Visuals")]
    public float lineLength = 14f;
    public float lineThickness = 2f;
    public float dotSize = 2f;
    public bool showCenterDot = true;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color enemyHoverColor = Color.red;

    [Header("ADS")]
    public float adsSpreadMultiplier = 0.4f;
}
