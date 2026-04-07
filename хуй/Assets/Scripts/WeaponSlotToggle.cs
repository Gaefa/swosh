using UnityEngine;

public class WeaponSlotToggle : MonoBehaviour
{
    [SerializeField] private GameObject heavyWeapon;

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (heavyWeapon != null)
                heavyWeapon.SetActive(!heavyWeapon.activeSelf);
        }
    }
#endif
}
