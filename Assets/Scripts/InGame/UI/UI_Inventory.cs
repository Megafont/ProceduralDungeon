using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

using ProceduralDungeon.InGame.Inventory;
using ProceduralDungeon.InGame.Items;


namespace ProceduralDungeon.InGame.UI
{
    public class UI_Inventory : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler    
    {
        public MouseDragItem _MouseDragItem;

        public InventoryObject InventoryToDisplay;
        public GameObject InventoryPrefab;

        public uint ItemSizeX = 64;
        public uint ItemSizeY = 64;
        public int TopLeftItemPositionX;
        public int TopLeftItemPositionY;
        public uint SpaceBetweenItemsX = 10;
        public uint SpaceBetweenItemsY = 10;
        public uint DisplayColumnCount = 5;

        public PointerEventData.InputButton _ClickButton = PointerEventData.InputButton.Left;
        public PointerEventData.InputButton _DragButton = PointerEventData.InputButton.Left;


        private Dictionary<GameObject, InventorySlot> _ItemsDisplayed;
        //List<GameObject> _ItemIconDisplayObjects;

        private bool _MouseIsOverInventoryPanel;





        // Start is called before the first frame update
        void Start()
        {
            _ItemsDisplayed = new Dictionary<GameObject, InventorySlot>();
            //_ItemIconDisplayObjects = new List<GameObject>();

            _MouseDragItem = new MouseDragItem();

            //UpdateDisplay();
            CreateSlots();
        }

        // Update is called once per frame
        void Update()
        {
            if (_ItemsDisplayed != null)
                UpdateSlots();

        }



        public void CreateSlots()
        {
            _ItemsDisplayed.Clear();

            for (int i = 0; i < InventoryToDisplay.Data.InventorySlots.Length; i++)
            {
                GameObject obj = Instantiate(InventoryPrefab, Vector3.zero, Quaternion.identity, transform);
                obj.GetComponent<RectTransform>().localPosition = GetIconPosition(i);

                AddButtonEvent(obj, EventTriggerType.PointerEnter, (data) => { OnMouseEnter((PointerEventData) data, obj); } );
                AddButtonEvent(obj, EventTriggerType.PointerExit, (data) => { OnMouseExit((PointerEventData) data, obj); });
                AddButtonEvent(obj, EventTriggerType.BeginDrag, (data) => { OnMouseStartDrag((PointerEventData) data, obj); });
                AddButtonEvent(obj, EventTriggerType.EndDrag, (data) => { OnMouseEndDrag((PointerEventData) data, obj); });
                AddButtonEvent(obj, EventTriggerType.Drag, (data) => { OnMouseDrag((PointerEventData) data, obj); });
                AddButtonEvent(obj, EventTriggerType.PointerUp, (data) => { OnMouseButtonUp((PointerEventData) data, obj); });

                _ItemsDisplayed.Add(obj, InventoryToDisplay.Data.InventorySlots[i]);
            }
        }

        public void UpdateSlots()
        {
            int i = 0;
            foreach (KeyValuePair<GameObject, InventorySlot> slot in _ItemsDisplayed)
            {
                Image slotImage = slot.Key.transform.GetChild(0).GetComponent<Image>();
                TextMeshProUGUI slotText = slot.Key.GetComponentInChildren<TextMeshProUGUI>();

                if (slot.Value.IsEmpty())
                {
                    slotImage.sprite = null;
                    slotImage.color = new Color(1, 1, 1, 0); // Set alpha to 0 to make the image invisible.
                    slotText.text = "";
                }
                else
                {
                    slotImage.sprite = InventoryToDisplay.ItemDatabase.LookupByID(slot.Value.Item.ID).Icon;
                    slotImage.color = new Color(1, 1, 1, 1);
                    slotText.text = slot.Value.ItemCount == 1 ? "" : slot.Value.ItemCount.ToString("n0");
                }

                slot.Key.transform.localPosition = GetIconPosition(i);
                i++;
            } // end foreach slot

        }

        private void AddButtonEvent(GameObject obj, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            EventTrigger trigger = obj.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = obj.AddComponent<EventTrigger>();

            EventTrigger.Entry eventTrigger = new EventTrigger.Entry();
            
            eventTrigger.eventID = type;
            eventTrigger.callback.AddListener(action);

            trigger.triggers.Add(eventTrigger);
        }

        private Vector3 GetIconPosition(int itemIndex)
        {
            int posX = TopLeftItemPositionX + (int)(ItemSizeX + SpaceBetweenItemsX) * (int)(itemIndex % DisplayColumnCount);
            int posY = TopLeftItemPositionY + (int)(ItemSizeY + SpaceBetweenItemsY) * (int)-(itemIndex / DisplayColumnCount); // The minus in this line makes it so each row appears below the previous. Otherwise it will stack item rows upward and go off the top of the panel.

            return new Vector3(posX, posY, 0f);

        }

        private bool ItemTypeIsDiscardable(ItemData item)
        {
            if (item is not ItemData_Key)
            {
                return true;
            }    
            else
            {
                return false;
            }
        }


        public void OnMouseEnter(PointerEventData eventData, GameObject obj)
        {
            _MouseDragItem.ObjectHoveredOver = obj;
            if (_ItemsDisplayed.ContainsKey(obj))
                _MouseDragItem.ItemOfSlotHoveredOver = _ItemsDisplayed[obj];
        }

        public void OnMouseExit(PointerEventData eventData, GameObject obj)
        {
            _MouseDragItem.ObjectHoveredOver = null;
            _MouseDragItem.ItemOfSlotHoveredOver = null;
        }

        public void OnMouseStartDrag(PointerEventData eventData, GameObject obj)
        {
            // Only allow a drag if the user is pressing the left mouse button.
            if (eventData.button != _DragButton)
                return;


            GameObject mouseObject = new GameObject();
            RectTransform rt = mouseObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ItemSizeX, ItemSizeY);
            mouseObject.transform.SetParent(transform.parent);

            if (_ItemsDisplayed[obj].Item != null)
            {
                Image image = mouseObject.AddComponent<Image>();
                image.sprite = InventoryToDisplay.ItemDatabase.LookupByID(_ItemsDisplayed[obj].Item.ID).Icon;
                image.raycastTarget = false;
            }

            _MouseDragItem.MouseDragCursorIcon = mouseObject;
            _MouseDragItem.MouseDragStartingItemSlot = _ItemsDisplayed[obj];
        }

        public void OnMouseEndDrag(PointerEventData eventData, GameObject obj)
        {
            // Only allow a drag if the user is pressing the left mouse button.
            if (eventData.button != _DragButton)
                return;


            InventorySlot itemSlot = _ItemsDisplayed[obj];

            if (_MouseDragItem.ObjectHoveredOver != null)
            {
                InventoryToDisplay.Data.SwapItem(itemSlot, _ItemsDisplayed[_MouseDragItem.ObjectHoveredOver]);
            }
            else
            {
                //Debug.LogError($"Mouse On Panel: {_MouseIsOverInventoryPanel}    Item Is Discarble: {ItemTypeIsDiscardable(itemSlot.Item)}");

                // Discard the item only if the player drags it outside the inventory window.
                if ((!_MouseIsOverInventoryPanel) && (ItemTypeIsDiscardable(itemSlot.Item)))
                {
                    if (itemSlot == null)
                        Debug.LogError("slot NULL!");
                    if (itemSlot.Item == null)
                        Debug.LogError("item NULL!");
                    InventoryToDisplay.Data.RemoveItem(itemSlot.Item.ID);
                }
            }

            Destroy(_MouseDragItem.MouseDragCursorIcon);
            _MouseDragItem.MouseDragStartingItemSlot = null;
        }

        public void OnMouseDrag(PointerEventData eventData, GameObject obj)
        {
            // Only allow a drag if the user is pressing the left mouse button.
            if (eventData.button != _DragButton)
                return;


            if (_MouseDragItem.MouseDragCursorIcon != null)
            {
                _MouseDragItem.MouseDragCursorIcon.GetComponent<RectTransform>().position = Input.mousePosition;
            }

        }

        public void OnMouseButtonUp(PointerEventData eventData, GameObject obj)
        {
            // If we are in a drag operation, then do nothing.
            if (_MouseDragItem.MouseDragCursorIcon != null)
                return;


            if (eventData.button == _ClickButton)
            {
                InventorySlot slot = _ItemsDisplayed[obj];

                InventoryToDisplay.SendItemClickedEventToInventoryOwner(_ItemsDisplayed[obj].Item);
            }
        }



        public void OnPointerEnter(PointerEventData eventData)
        {
            //Debug.LogError("MOUSE ENTER - " + eventData.pointerCurrentRaycast.gameObject.name);
            
            _MouseIsOverInventoryPanel = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _MouseIsOverInventoryPanel = false;
        }


    }


}



public class MouseDragItem
{
    public GameObject MouseDragCursorIcon;
    public InventorySlot MouseDragStartingItemSlot;
    public GameObject ObjectHoveredOver;
    public InventorySlot ItemOfSlotHoveredOver;
}
