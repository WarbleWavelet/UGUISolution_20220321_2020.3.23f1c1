using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;
 
namespace Demo0_0
{
    public class ClickUI_IPointerClickHandler_IsUI: MonoBehaviour
    {
        private int _index;
        private GraphicRaycaster _raycaster;
        // Start is called before the first frame update
        void Start()
        {
            _raycaster = FindObjectOfType<GraphicRaycaster>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0) && !IsUI())
            {
                ChangeColor();
            }
        }

        void ChangeColor()
        {
            if (_index == 0)
            {
                GetComponent<MeshRenderer>().material.SetColor("_Color", Color.black);
            }
            else
            {
                GetComponent<MeshRenderer>().material.SetColor("_Color", Color.white);
            }
            _index = _index == 0 ? 1 : 0;
        }

        private bool IsUI()
        {
            PointerEventData data = new PointerEventData(EventSystem.current);
            data.pressPosition = Input.mousePosition;
            data.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            _raycaster.Raycast(data, results);
            return results.Count > 0;//点击UI
        }
    }
}