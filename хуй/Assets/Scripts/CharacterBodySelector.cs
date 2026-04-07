using UnityEngine;

public class CharacterBodySelector : MonoBehaviour
{
    public enum BodyType
    {
        Undead,
        Paladin
    }

    [Header("Bodies")]
    public GameObject undeadBody;
    public GameObject paladinBody;

    [Header("Animators")]
    public Animator undeadAnimator;
    public Animator paladinAnimator;

    [Header("Links to update")]
    public MobileMovement mobileMovement;
    public PlayerHealth playerHealth;
    public MobileShoot mobileShoot;

    [Header("Start")]
    public BodyType startBody = BodyType.Undead;

    private BodyType currentBody;

    private void Awake()
    {
        ApplyBody(startBody);
    }

    public void SetUndead()
    {
        ApplyBody(BodyType.Undead);
    }

    public void SetPaladin()
    {
        ApplyBody(BodyType.Paladin);
    }

    public void ApplyBody(BodyType bodyType)
    {
        currentBody = bodyType;

        bool usePaladin = bodyType == BodyType.Paladin;

        if (undeadBody != null)
            undeadBody.SetActive(!usePaladin);

        if (paladinBody != null)
            paladinBody.SetActive(usePaladin);

        Animator activeAnimator = usePaladin ? paladinAnimator : undeadAnimator;

        if (mobileMovement != null)
            mobileMovement.tppAnimator = activeAnimator;

        if (playerHealth != null)
            playerHealth.tppAnimator = activeAnimator;

        if (mobileShoot != null)
            mobileShoot.SendMessage("SetTPPAnimator", activeAnimator, SendMessageOptions.DontRequireReceiver);
    }

    public BodyType GetCurrentBody()
    {
        return currentBody;
    }
}
