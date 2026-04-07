using UnityEngine;

public class CharacterSkinSwitcher : MonoBehaviour
{
    [Header("Body Renderer")]
    public SkinnedMeshRenderer bodyRenderer;

    [Header("Materials")]
    public Material undeadMaterial;
    public Material paladinMaterial;

    private void Awake()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
    }

    private void Update()
    {
        // P = Paladin
        if (Input.GetKeyDown(KeyCode.P))
        {
            SetPaladinSkin();
        }

        // U = Undead
        if (Input.GetKeyDown(KeyCode.U))
        {
            SetUndeadSkin();
        }
    }

    public void SetUndeadSkin()
    {
        if (bodyRenderer == null || undeadMaterial == null) return;
        bodyRenderer.material = undeadMaterial;
    }

    public void SetPaladinSkin()
    {
        if (bodyRenderer == null || paladinMaterial == null) return;
        bodyRenderer.material = paladinMaterial;
    }
}