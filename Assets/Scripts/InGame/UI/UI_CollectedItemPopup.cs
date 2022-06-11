using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using TMPro;

using ProceduralDungeon.TileMaps;


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
        public ItemTypes ItemType;

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



        public void SetItemType(ItemData itemData, RoomSets roomSet)
        {
            string itemName = Enum.GetName(typeof(ItemTypes), itemData.ItemType);

            _SpriteRenderer.sprite = SpriteLoader.GetItemSprite(itemName, roomSet);
            TMP_TextComponent.text = FormatItemName(itemName, itemData.ItemCount);
            TMP_TextComponent.color = GetTextColor(itemData.ItemType);
            
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
            string result = count + "x " + itemName.Substring(5, itemName.Length - 5);
            result = result.Replace('_', ' ');

            return result;
        }

        private Color32 GetTextColor(ItemTypes itemType)
        {
            Color32 color = Color.white;


            switch (itemType)
            {
                case ItemTypes.Item_Bomb:
                    color = new Color(0, 0, 200, 255); break;

                case ItemTypes.Item_Key:
                    color = new Color(200, 255, 0, 255); break;
                case ItemTypes.Item_Key_Part:
                    color = new Color(0, 255, 255, 255); break;
                case ItemTypes.Item_Key_Goal:
                    color = new Color(255, 0, 0, 255); break;
}

            return color;
        }


    }


}

