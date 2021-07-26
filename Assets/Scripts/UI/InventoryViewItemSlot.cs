using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryViewItemSlot : InterfaceItemView, IPointerClickHandler
{
    [SerializeField] private PlayerInventory _playerInventory;
    public void OnPointerClick(PointerEventData eventData)
    {
        _playerInventory.InventoryInteract(eventData.button, InventoryItemSlotIndex);
    }
}
