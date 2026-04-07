using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemCardUI : MonoBehaviour
{
    public Image previewImage;
    public Image bottomBar;
    public TextMeshProUGUI nameText;

    public void Setup(ItemData data)
    {
        nameText.text = data.itemName;
        bottomBar.color = data.rarityColor;
        previewImage.sprite = data.icon;
    }
}