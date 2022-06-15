using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using ProceduralDungeon.InGame.Inventory;


namespace ProceduralDungeon.InGame.UI
{
    public class UI_Inventory : MonoBehaviour
    {
        public InventoryObject InventoryToDisplay;
        public GameObject InventoryPrefab;

        public uint ItemSizeX = 64;
        public uint ItemSizeY = 64;
        public int TopLeftItemPositionX;
        public int TopleftItemPositionY;
        public uint SpaceBetweenItemsX = 10;
        public uint SpaceBetweenItemsY = 10;
        public uint DisplayColumnCount = 5;


        Dictionary<InventorySlot, GameObject> _ItemsDisplayed;
        List<GameObject> _ItemIconDisplayObjects;
        


        // Start is called before the first frame update
        void Start()
        {
            _ItemsDisplayed = new Dictionary<InventorySlot, GameObject>();
            _ItemIconDisplayObjects = new List<GameObject>();

            UpdateDisplay();
        }

        // Update is called once per frame
        void Update()
        {
           UpdateDisplay();
        }



        public void UpdateDisplay()
        {
            InventoryData contents = InventoryToDisplay.Data;

            int itemCount = contents.Items.Count;
            int iconCount = _ItemIconDisplayObjects.Count;

            int i = 0;
            while (true)
            {
                if (i >= iconCount && i >= itemCount)
                    break;


                if (i < itemCount)
                {
                    InventorySlot slot = InventoryToDisplay.Data.Items[i];

                    if (i < iconCount)
                        UpdateItemIcon(_ItemIconDisplayObjects[i], i);
                    else
                        CreateItemIcon(slot, i);
                }               
                else
                {
                    UpdateItemIcon(_ItemIconDisplayObjects[i], i);
                }

                i++;

            } // end while

        }



        private GameObject CreateItemIcon(InventorySlot slot, int itemIndex)
        {
            GameObject obj = Instantiate(InventoryPrefab, Vector3.zero, Quaternion.identity, transform);
            _ItemIconDisplayObjects.Add(obj);

            UpdateItemIcon(obj, itemIndex);

            return obj;
        }

        private void UpdateItemIcon(GameObject itemIcon, int iconIndex)
        {
            InventorySlot itemSlot = null;
            if (iconIndex >= InventoryToDisplay.Data.Items.Count)
            {
                // This UI icon is currently unused, so just hide it until we need it again.
                _ItemIconDisplayObjects[iconIndex].SetActive(false);
            }
            else
            {
                _ItemIconDisplayObjects[iconIndex].SetActive(true);

                itemSlot = InventoryToDisplay.Data.Items[iconIndex];


                // Set the position of the item icon in the inventory panel.
                itemIcon.GetComponent<RectTransform>().localPosition = GetPosition(iconIndex);


                // Set the background sprite's size.
                Image background = itemIcon.GetComponent<Image>();
                background.rectTransform.sizeDelta = new Vector2(ItemSizeX, ItemSizeY);


                // Setup the item's icon.
                Image icon = itemIcon.transform.GetChild(0).GetComponentInChildren<Image>();
                icon.rectTransform.sizeDelta = new Vector2(ItemSizeX, ItemSizeY);
                icon.sprite = InventoryToDisplay.ItemDatabase.LookupByID(itemSlot.Item.ID).Icon;


                // Set the item count text and make its text field occupy the lower-right corner of the image (half the width and height of the item icon).
                TextMeshProUGUI tmpText = itemIcon.GetComponentInChildren<TextMeshProUGUI>();
                tmpText.rectTransform.localPosition = new Vector2(ItemSizeX * 0.25f, -ItemSizeY * 0.25f);
                tmpText.rectTransform.sizeDelta = new Vector2(ItemSizeX * 0.5f, ItemSizeY * 0.5f);
                tmpText.text = itemSlot.ItemCount.ToString("n0");

            }
        }

        private Vector3 GetPosition(int itemIndex)
        {
            int posX = TopLeftItemPositionX + (int)(ItemSizeX + SpaceBetweenItemsX) * (int)(itemIndex % DisplayColumnCount);
            int posY = TopleftItemPositionY + (int)(ItemSizeY + SpaceBetweenItemsY) * (int)-(itemIndex / DisplayColumnCount); // The minus in this line makes it so each row appears below the previous. Otherwise it will stack item rows upward and go off the top of the panel.

            return new Vector3(posX, posY, 0f);

        }


    }


}
