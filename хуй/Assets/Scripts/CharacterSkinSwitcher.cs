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

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            SetPaladinSkin();

        if (Input.GetKeyDown(KeyCode.U))
            SetUndeadSkin();
    }
#endif

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