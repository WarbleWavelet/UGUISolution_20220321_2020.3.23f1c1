using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RotationDiagramItem : MonoBehaviour,IDragHandler,IEndDragHandler
{


    #region 字段
    /// <summary>顺序</summary>
    public int PosId;
    /// <summary>偏移量</summary>
    private Action<float> _moveAction;
    private Image _image;
    private float _offsetX;
    /// <summary>拖动时动画持续时间</summary>
    private float _aniTime = 1;
    private RectTransform _rect;
    #endregion


    #region 属性、属性方法
    private Image Image
    {
        get
        {
            if (_image == null)
                _image = GetComponent<Image>();

            return _image;
        }
    }
    private RectTransform Rect
    {
        get
        {
            if (_rect == null)
                _rect = GetComponent<RectTransform>();

            return _rect;
        }
    }

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent);
    }

    public void SetSprite(Sprite sprite)
    {
        Image.sprite = sprite;
    }
    #endregion
    #region 重写
    public void OnDrag(PointerEventData eventData)
    {
        _offsetX += eventData.delta.x;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _moveAction(_offsetX);
        _offsetX = 0;
    }
    #endregion
    public void AddMoveListener(Action<float> onMove)
    {
        _moveAction = onMove;
    }

    public void ChangeId(int symbol,int totalItemNum)
    {
        //比如一下
        int id = PosId;//0；7
        id += symbol;//0-1=-1；7+1=8
        if (id < 0)
        {
            id += totalItemNum;//-1+8=7；8+8=16
        }
        PosId = id%totalItemNum;//16&8=0
    }

    public void SetPosData(ItemPosData data)
    {
        Rect.DOAnchorPos(Vector2.right * data.X, _aniTime);
        Rect.DOScale(Vector3.one * data.ScaleTimes, _aniTime);

        StartCoroutine(Wait(data));
    }

    /// <summary>
    /// 防止重叠时穿叉
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private IEnumerator Wait(ItemPosData data)
    {
        yield return new WaitForSeconds(_aniTime * 0.5f);
        transform.SetSiblingIndex(data.Order);
    }
}
