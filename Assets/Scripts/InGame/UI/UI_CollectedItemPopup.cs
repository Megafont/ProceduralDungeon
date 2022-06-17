using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using TMPro;

using ProceduralDungeon.InGame.Inventory;
using ProceduralDungeon.InGame.Items;
using ProceduralDungeon.TileMaps;
using ProceduralDungeon.Utilities;


namespace ProceduralDungeon.InGame.UI
{

    public class UI_CollectedItemPopup : MonoBehaviour
    {
        [SerializeField]
        AnimationCurve AccelerationCurve;
              
        [SerializeField]
        [Range(0f, 5f)]
        float FadeStartDelay = 1.0f;

        [SerializeField]
        [Range(0f, 5f)]
        float FadeOutTime = 1.0f;

        [SerializeField]
        [Range(0f, 5f)]
        float MaxMoveSpeed = 2.5f;

        [SerializeField]
        TMP_Text TMP_TextComponent;



        private float _ElapsedTime;
        private float _MoveSpeed;

        private SpriteRenderer _SpriteRenderer;



        // Start is called before the first frame update
        void Awake()
        {
            _SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            _ElapsedTime += Time.deltaTime;

            if (_ElapsedTime <= FadeStartDelay)
            {
                float curveResult = AccelerationCurve.Evaluate(_ElapsedTime / FadeStartDelay);

                _MoveSpeed = Mathf.Max(_MoveSpeed, (MaxMoveSpeed * curveResult));
            }
            else if (_ElapsedTime - FadeStartDelay <= FadeOutTime)
            {
                byte alpha = (byte) (((_ElapsedTime - FadeStartDelay) / FadeOutTime) * 255f);
                alpha = (byte) (255 - alpha);

                Color32 color = TMP_TextComponent.color;
                color.a = alpha;
                TMP_TextComponent.color = color;

                color = _SpriteRenderer.color;
                color.a = alpha;
                _SpriteRenderer.color = color;
            }
            else
            {
                Destroy(gameObject);
            }


            transform.position += Vector3.up * _MoveSpeed * Time.deltaTime;

        }



        public void SetItem(InventorySlot slot, RoomSets roomSet)
        {
            string itemName = slot.Item.Name;

            _SpriteRenderer.sprite = SpriteManager.GetItemSprite(itemName, roomSet);
            TMP_TextComponent.text = FormatItemName(itemName, slot.ItemCount);
            TMP_TextComponent.color = GetTextColor(itemName);
            
        }

        public void SetPopupText(string text)
        {
            TMP_TextComponent.text = text;
        }

        public void SetPopupSprite(Sprite sprite)
        {
            _SpriteRenderer.sprite = sprite;
        }

        private string FormatItemName(string itemName, uint count)
        {
            string result = count + "x " + itemName;
            result = result.Replace('_', ' ');

            return result;
        }

        private Color32 GetTextColor(string itemName)
        {
            Color32 color = Color.white;


            switch (itemName)
            {
                case "Bomb":
                    color = new Color(0, 0, 200, 255); break;

                case "Key":
                    color = new Color(200, 255, 0, 255); break;
                case "Key Part":
                    color = new Color(0, 255, 255, 255); break;
                case "Goal Key":
                    color = new Color(255, 0, 0, 255); break;
}

            return color;
        }


    }


}

