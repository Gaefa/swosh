using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button btnMain;
    public Button btnStore;
    public Button btnFriends;

    [Header("Main Menu Panels")]
    public GameObject profilePanel;
    public GameObject storePanel;
    public GameObject friendsPanel;

    [Header("Store Tabs Buttons")]
    public Button btnInventory;
    public Button btnWeaponStore;
    public Button btnCurrency;

    [Header("Store Tabs Panels")]
    public GameObject inventoryPanel;
    public GameObject weaponStorePanel;
    public GameObject currencyPanel;

    [Header("Colors")]
    public Color activeColor = new Color(1f, 0.2f, 0.2f, 1f);
    public Color inactiveColor = new Color(0.3f, 0f, 0f, 1f);
    public Color storeTabActiveColor = new Color(120f / 255f, 20f / 255f, 20f / 255f, 1f);
    public Color storeTabInactiveColor = new Color(40f / 255f, 40f / 255f, 40f / 255f, 1f);

    void Start()
    {
        // Главное меню
        btnMain.onClick.AddListener(() =>
        {
            SetMainMenuActive(btnMain);
            ShowProfile();
        });

        btnStore.onClick.AddListener(() =>
        {
            SetMainMenuActive(btnStore);
            ShowStore();
        });

        btnFriends.onClick.AddListener(() =>
        {
            SetMainMenuActive(btnFriends);
            ShowFriends();
        });

        // Вкладки магазина
        if (btnInventory != null)
        {
            btnInventory.onClick.AddListener(() =>
            {
                SetStoreTabActive(btnInventory);
                ShowInventoryTab();
            });
        }

        if (btnWeaponStore != null)
        {
            btnWeaponStore.onClick.AddListener(() =>
            {
                SetStoreTabActive(btnWeaponStore);
                ShowWeaponStoreTab();
            });
        }

        if (btnCurrency != null)
        {
            btnCurrency.onClick.AddListener(() =>
            {
                SetStoreTabActive(btnCurrency);
                ShowCurrencyTab();
            });
        }

        // Стартовое состояние
        SetMainMenuActive(btnMain);
        ShowProfile();

        if (storePanel != null)
        {
            SetStoreTabActive(btnInventory);
            ShowInventoryTab();
        }
    }

    void SetMainMenuActive(Button activeBtn)
    {
        SetButtonColor(btnMain, inactiveColor);
        SetButtonColor(btnStore, inactiveColor);
        SetButtonColor(btnFriends, inactiveColor);

        SetButtonColor(activeBtn, activeColor);
    }

    void SetStoreTabActive(Button activeBtn)
    {
        if (btnInventory != null) SetButtonColor(btnInventory, storeTabInactiveColor);
        if (btnWeaponStore != null) SetButtonColor(btnWeaponStore, storeTabInactiveColor);
        if (btnCurrency != null) SetButtonColor(btnCurrency, storeTabInactiveColor);

        if (activeBtn != null) SetButtonColor(activeBtn, storeTabActiveColor);
    }

    void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;

        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = color;

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.gray;
        btn.colors = colors;
    }

    // ===== MAIN PANELS =====

    public void ShowProfile()
    {
        if (profilePanel != null) profilePanel.SetActive(true);
        if (storePanel != null) storePanel.SetActive(false);
        if (friendsPanel != null) friendsPanel.SetActive(false);
    }

    public void ShowStore()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
        if (storePanel != null) storePanel.SetActive(true);
        if (friendsPanel != null) friendsPanel.SetActive(false);

        SetStoreTabActive(btnInventory);
        ShowInventoryTab();
    }

    public void ShowFriends()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
        if (storePanel != null) storePanel.SetActive(false);
        if (friendsPanel != null) friendsPanel.SetActive(true);
    }

    // ===== STORE TABS =====

    public void ShowInventoryTab()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (weaponStorePanel != null) weaponStorePanel.SetActive(false);
        if (currencyPanel != null) currencyPanel.SetActive(false);
    }

    public void ShowWeaponStoreTab()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (weaponStorePanel != null) weaponStorePanel.SetActive(true);
        if (currencyPanel != null) currencyPanel.SetActive(false);
    }

    public void ShowCurrencyTab()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (weaponStorePanel != null) weaponStorePanel.SetActive(false);
        if (currencyPanel != null) currencyPanel.SetActive(true);
    }

    // ===== PLAY =====

    public void OnPlayPressed()
    {
        SceneManager.LoadScene(1);
    }
}