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


        Dictionary<InventorySlot, GameObject> _ItemsDisplayed = new Dictionary<InventorySlot, GameObject>();

        

        // Start is called before the first frame update
        void Start()
        {
            CreateDisplay();
        }

        // Update is called once per frame
        void Update()
        {
           UpdateDisplay();
        }



        public void UpdateDisplay()
        {
            InventoryData contents = InventoryToDisplay.Contents;

            for (int i = 0; i < contents.Items.Count; i++)
            {
                InventorySlot slot = InventoryToDisplay.Contents.Items[i];

                if (_ItemsDisplayed.ContainsKey(contents.Items[i]))
                {
                    _ItemsDisplayed[InventoryToDisplay.Contents.Items[i]].GetComponentInChildren<TextMeshProUGUI>().text = contents.Items[i].ItemCount.ToString("n0");
                }
                else
                {
                    GameObject iconPrefab = CreateItemIcon(slot, i);

                    /*
                    GameObject obj = Instantiate(InventoryPrefab, Vector3.zero, Quaternion.identity, transform);
                    obj.transform.GetChild(0).GetComponentInChildren<Image>().sprite = InventoryToDisplay.ItemDatabase.ItemLookup[slot.Item.ID].Icon;
                    obj.GetComponent<RectTransform>().localPosition = GetPosition(i);
                    obj.GetComponentInChildren<TextMeshProUGUI>().text = slot.ItemCount.ToString("n0");
                    */

                    _ItemsDisplayed.Add(contents.Items[i], iconPrefab);
                }
            }

        }

        public void CreateDisplay()
        {
            InventoryData contents = InventoryToDisplay.Contents;

            for (int i = 0; i < contents.Items.Count; i++)
            {
                InventorySlot slot = InventoryToDisplay.Contents.Items[i];

                GameObject iconPrefab = CreateItemIcon(slot, i);
                /*
                GameObject obj = Instantiate(InventoryPrefab, Vector3.zero, Quaternion.identity, transform);
                obj.transform.GetChild(0).GetComponentInChildren<Image>().sprite = InventoryToDisplay.ItemDatabase.ItemLookup[slot.Item.ID].Icon;
                obj.GetComponent<RectTransform>().localPosition = GetPosition(i);
                obj.GetComponentInChildren<TextMeshProUGUI>().text = slot.ItemCount.ToString("n0");
                */

                _ItemsDisplayed.Add(slot, iconPrefab);
            }
        }

        private GameObject CreateItemIcon(InventorySlot slot, int itemIndex)
        {
            GameObject obj = Instantiate(InventoryPrefab, Vector3.zero, Quaternion.identity, transform);


            // Set the background sprite's size.
            Image background = obj.GetComponent<Image>();
            background.rectTransform.sizeDelta = new Vector2(ItemSizeX, ItemSizeY);

            // Setup the item's icon.
            Image icon = obj.transform.GetChild(0).GetComponentInChildren<Image>();
            icon.rectTransform.sizeDelta = new Vector2(ItemSizeX, ItemSizeY);
            icon.sprite = InventoryToDisplay.ItemDatabase.ItemLookup[slot.Item.ID].Icon;

            // Set the position of the item icon in the inventory panel.
            obj.GetComponent<RectTransform>().localPosition = GetPosition(itemIndex);

            // Set the item count text and make its text field occupy the lower-right corner of the image (half the width and height of the item icon).
            TextMeshProUGUI tmpText = obj.GetComponentInChildren<TextMeshProUGUI>();
            tmpText.rectTransform.localPosition = new Vector2(ItemSizeX * 0.25f, -ItemSizeY * 0.25f);
            tmpText.rectTransform.sizeDelta = new Vector2(ItemSizeX * 0.5f, ItemSizeY * 0.5f);
            tmpText.text = slot.ItemCount.ToString("n0");
            

            return obj;
        }


        public Vector3 GetPosition(int itemIndex)
        {
            int posX = (int) TopLeftItemPositionX + (int) (ItemSizeX + SpaceBetweenItemsX) * (int) (itemIndex % DisplayColumnCount);
            int posY = (int) TopleftItemPositionY + (int) (ItemSizeY + SpaceBetweenItemsY) * (int) (itemIndex / DisplayColumnCount);
            
            return new Vector3(posX, posY, 0f);

        }
        
    }


}
