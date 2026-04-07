using UnityEngine;

public class WeaponSlotToggle : MonoBehaviour
{
    [SerializeField] private GameObject heavyWeapon; // ссылка на heavy_gun_blockot_v1

    private void Update()
    {
        // ТЕСТ: кнопка "1" включает/выключает heavy
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (heavyWeapon != null)
                heavyWeapon.SetActive(!heavyWeapon.activeSelf);
        }
    }
}