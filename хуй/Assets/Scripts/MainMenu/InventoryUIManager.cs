using System.Collections.Generic;
using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    [Header("References")]
    public Transform gridParent;      // InventoryGrid
    public GameObject itemCardPrefab; // ItemCard prefab

    [Header("Test Data")]
    public List<ItemData> items = new List<ItemData>();

    void Start()
    {
        GenerateItems();
    }

    void GenerateItems()
    {
        foreach (var item in items)
        {
            GameObject obj = Instantiate(itemCardPrefab, gridParent);

            ItemCardUI card = obj.GetComponent<ItemCardUI>();
            card.Setup(item);
        }
    }
}