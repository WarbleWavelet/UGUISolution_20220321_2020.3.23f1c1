using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
 
namespace Demo01_01
{
    public class Click3D_OnMouseDown : MonoBehaviour
    {
        private int _index=0;





        public void OnMouseDown()
        {

            ChangeColor();
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
    }
}