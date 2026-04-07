using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TPPArmedToggle : MonoBehaviour
{
    [System.Serializable]
    public class WeaponIKProfile
    {
        [Header("Weapon")]
        public int weaponId;                 // 1=fast, 2=heavy, 3=roklet
        public GameObject weaponGO;          // объект оружия внутри WeaponHolder

        [Header("This Weapon Rig")]
        public Rig weaponRig;                // TPP_Rig_Fast / Heavy / Roklet
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;

        [Header("Grip / Hint names for THIS weapon")]
        public string rightGripName = "RightGrip";
        public string leftGripName = "LeftGrip";
        public string rightElbowHintName = "RightElbowHint";
        public string leftElbowHintName = "LeftElbowHint";

        [Header("IK Weights: IDLE")]
        [Range(0f, 1f)] public float idle_constraintWeight = 1f;
        [Range(0f, 1f)] public float idle_targetPosWeight = 1f;
        [Range(0f, 1f)] public float idle_targetRotWeight = 1f;
        [Range(0f, 1f)] public float idle_hintWeight = 1f;

        [Header("IK Weights: WALK")]
        [Range(0f, 1f)] public float walk_constraintWeight = 1f;
        [Range(0f, 1f)] public float walk_targetPosWeight = 1f;
        [Range(0f, 1f)] public float walk_targetRotWeight = 1f;
        [Range(0f, 1f)] public float walk_hintWeight = 1f;
    }

    [Header("Rig Builder / Holder")]
    public RigBuilder rigBuilder;
    public GameObject weaponHolderTPP;

    [Header("Drive MOVING")]
    public bool driveFromAnimatorBool = true;
    public Animator tppAnimator;
    public string isMovingBoolName = "IsMoving";
    public float blendSpeed = 12f;

    [Header("Weapon Profiles")]
    public WeaponIKProfile[] profiles;

    [Header("Start State")]
    public bool startArmed = false;
    [Range(0, 3)] public int startWeaponId = 0;

    private int currentWeaponId = 0;
    private WeaponIKProfile currentProfile;
    private float moveBlend01 = 0f;
    private bool movingOverride = false;
    private int isMovingHash;

    private void Awake()
    {
        isMovingHash = Animator.StringToHash(isMovingBoolName);

        if (rigBuilder == null)
            rigBuilder = GetComponentInParent<RigBuilder>(true);

        ApplyWeapon(startWeaponId, startArmed);
    }

    public void SetMoving(bool moving)
    {
        movingOverride = moving;
    }

    private void LateUpdate()
    {
        if (currentProfile == null) return;

        bool moving = false;

        if (driveFromAnimatorBool)
        {
            if (tppAnimator == null) return;
            moving = tppAnimator.GetBool(isMovingHash);
        }
        else
        {
            moving = movingOverride;
        }

        float target = moving ? 1f : 0f;
        moveBlend01 = Mathf.MoveTowards(moveBlend01, target, Time.deltaTime * blendSpeed);

        ApplyProfileWeights(currentProfile, moveBlend01);

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetWeaponId(0);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetWeaponId(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetWeaponId(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetWeaponId(3);
#endif
    }

    public void SetWeaponId(int weaponId)
    {
        ApplyWeapon(weaponId, weaponId != 0);
    }

    public void SetArmed(bool armed)
    {
        ApplyWeapon(currentWeaponId, armed);
    }

    private void ApplyWeapon(int weaponId, bool armed)
    {
        currentWeaponId = Mathf.Clamp(weaponId, 0, 3);

        if (weaponHolderTPP != null)
            weaponHolderTPP.SetActive(armed);

        DisableAllProfiles();

        currentProfile = null;

        if (!armed || currentWeaponId == 0)
        {
            RebuildRigNow();
            return;
        }

        currentProfile = FindProfile(currentWeaponId);
        if (currentProfile == null)
        {
            RebuildRigNow();
            return;
        }

        if (currentProfile.weaponGO != null)
            currentProfile.weaponGO.SetActive(true);

        if (currentProfile.weaponRig != null)
        {
            currentProfile.weaponRig.gameObject.SetActive(true);
            currentProfile.weaponRig.weight = 1f;
        }

        if (!BindIkToWeapon(currentProfile))
        {
            ClearProfileIK(currentProfile);
            RebuildRigNow();
            return;
        }

        bool movingNow = driveFromAnimatorBool && tppAnimator != null
            ? tppAnimator.GetBool(isMovingHash)
            : movingOverride;

        moveBlend01 = movingNow ? 1f : 0f;
        ApplyProfileWeights(currentProfile, moveBlend01);

        RebuildRigNow();
    }

    private WeaponIKProfile FindProfile(int weaponId)
    {
        if (profiles == null) return null;

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i] != null && profiles[i].weaponId == weaponId)
                return profiles[i];
        }

        return null;
    }

    private bool BindIkToWeapon(WeaponIKProfile profile)
    {
        if (profile == null || profile.weaponGO == null) return false;

        Transform rGrip = FindChildRecursive(profile.weaponGO.transform, profile.rightGripName);
        Transform lGrip = FindChildRecursive(profile.weaponGO.transform, profile.leftGripName);
        Transform rHint = FindChildRecursive(profile.weaponGO.transform, profile.rightElbowHintName);
        Transform lHint = FindChildRecursive(profile.weaponGO.transform, profile.leftElbowHintName);

        if (profile.rightHandIK != null && rGrip == null) return false;
        if (profile.leftHandIK != null && lGrip == null) return false;

        if (profile.rightHandIK != null)
        {
            profile.rightHandIK.data.target = rGrip;
            profile.rightHandIK.data.hint = rHint;
        }

        if (profile.leftHandIK != null)
        {
            profile.leftHandIK.data.target = lGrip;
            profile.leftHandIK.data.hint = lHint;
        }

        return true;
    }

    private void ApplyProfileWeights(WeaponIKProfile p, float move01)
    {
        if (p == null) return;

        float cW = Mathf.Lerp(p.idle_constraintWeight, p.walk_constraintWeight, move01);
        float posW = Mathf.Lerp(p.idle_targetPosWeight, p.walk_targetPosWeight, move01);
        float rotW = Mathf.Lerp(p.idle_targetRotWeight, p.walk_targetRotWeight, move01);
        float hW = Mathf.Lerp(p.idle_hintWeight, p.walk_hintWeight, move01);

        ApplyToConstraint(p.rightHandIK, cW, posW, rotW, hW);
        ApplyToConstraint(p.leftHandIK, cW, posW, rotW, hW);
    }

    private void ApplyToConstraint(TwoBoneIKConstraint c, float cW, float posW, float rotW, float hintW)
    {
        if (c == null) return;

        c.weight = cW;
        c.data.targetPositionWeight = posW;
        c.data.targetRotationWeight = rotW;
        c.data.hintWeight = hintW;
    }

    private void DisableAllProfiles()
    {
        if (profiles == null) return;

        for (int i = 0; i < profiles.Length; i++)
        {
            WeaponIKProfile p = profiles[i];
            if (p == null) continue;

            if (p.weaponGO != null)
                p.weaponGO.SetActive(false);

            if (p.weaponRig != null)
            {
                p.weaponRig.weight = 0f;
                p.weaponRig.gameObject.SetActive(false);
            }

            ClearProfileIK(p);
        }
    }

    private void ClearProfileIK(WeaponIKProfile p)
    {
        if (p == null) return;

        if (p.rightHandIK != null)
        {
            p.rightHandIK.data.target = null;
            p.rightHandIK.data.hint = null;
            p.rightHandIK.weight = 0f;
        }

        if (p.leftHandIK != null)
        {
            p.leftHandIK.data.target = null;
            p.leftHandIK.data.hint = null;
            p.leftHandIK.weight = 0f;
        }
    }

    private void RebuildRigNow()
    {
        if (rigBuilder == null) return;
        rigBuilder.Build();
    }

    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }
}