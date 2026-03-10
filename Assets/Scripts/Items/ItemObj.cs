using UnityEngine;
using UnityEngine.UI;

// Simple UI helper component to attach to item UI prefab instances.
// The button or EventTrigger on the prefab can call OnClick() to notify GameStreamManager.
public class ItemObj : MonoBehaviour
{
    public ItemSO item;
    public Image iconImage;

    private void Start()
    {
        iconImage.sprite = item.icon;
    }
    public void OnClick()
    {
        if (item == null) return;
        GameStreamManager.Instance.ShowItemDescription(item);
    }
}
