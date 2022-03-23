using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;
 
namespace Demo0_0
{
    public class ClickUI_IPointerClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private int _index;



        public void OnPointerClick(PointerEventData eventData)
        {
            ChangeColor();
        }

        public void ChangeColor()
        {
            if (_index == 0)
            {
                GetComponent<Image>().color = Color.blue;
            }
            else
            {
                GetComponent<Image>().color = Color.white;
            }
            _index = _index == 0 ? 1 : 0;
        }

    }
}