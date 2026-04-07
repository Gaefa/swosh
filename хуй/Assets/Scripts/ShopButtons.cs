using UnityEngine;

public class ShopButtons : MonoBehaviour
{
    public MobileShoot playerShoot;

    private void Awake()
    {
        if (playerShoot == null)
            playerShoot = FindObjectOfType<MobileShoot>();

        Debug.Log($"[ShopButtons] Awake | playerShoot={(playerShoot ? playerShoot.name : "NULL")}");
    }

    public void PickFast()
    {
        Debug.Log("[ShopButtons] PickFast CLICK");
        if (playerShoot == null) { Debug.LogWarning("[ShopButtons] playerShoot NULL"); return; }
        playerShoot.SetWeapon(WeaponType.Fast);
    }

    public void PickHeavy()
    {
        Debug.Log("[ShopButtons] PickHeavy CLICK");
        if (playerShoot == null) { Debug.LogWarning("[ShopButtons] playerShoot NULL"); return; }
        playerShoot.SetWeapon(WeaponType.Heavy);
    }

    public void PickRoklet()
    {
        Debug.Log("[ShopButtons] PickRoklet CLICK");
        if (playerShoot == null) { Debug.LogWarning("[ShopButtons] playerShoot NULL"); return; }
        playerShoot.SetWeapon(WeaponType.Roklet);
    }
}