using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

public class CircleImage : Image
{

    #region 字段
    /// <summary> 圆形由多少块三角形拼成 </summary>
    [SerializeField]
    private int segements = 5;
    /// <summary>显示部分占圆形的百分比0-1</summary>
    [SerializeField]
    private float showPercent = 1;
    /// <summary>遮罩的颜色，想想技能冷却</summary>
    private readonly Color32 MASK_COLOR = new Color32(60, 60, 60, 255);
    /// <summary>处理被遮罩不可点击</summary>
    private List<Vector3> _vertexList;
    #endregion

    #region 重写
    /// <summary>
    /// 
    /// </summary>
    /// <param name="vh">给GPU读取的数据载体</param>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        _vertexList = new List<Vector3>();

        AddVertex(vh, segements);

        AddTriangle(vh, segements);
    }

    /// <summary>
    /// 使得被遮罩的部分，点击不会响应
    /// </summary>
    /// <param name="screenPoint"></param>
    /// <param name="eventCamera"></param>
    /// <returns></returns>
    public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out localPoint);
        return IsValid(localPoint);
    }
    #endregion


    #region 辅助2
    /// <summary>
    /// 片元之前的点坐标
    /// </summary>
    /// <param name="vh">给GPU读取的数据载体</param>
    /// <param name="segements">片元数</param>
    private void AddVertex(VertexHelper vh, int segements)
    {
        GetRectWH(rectTransform, out float width, out float height);
        GetSpriteWHC(overrideSprite, out float uvWidth, out float uvHeight, out Vector2 uvCenter);
        //
        Color32 colorTemp = GetOriginColor(showPercent);
        GetOriginPos(width, height, out  Vector2 originPos);
        Vector2 vertPos = Vector2.zero;
        Vector2 rect2UVRatio = new Vector2(uvWidth / width, uvHeight / height);//rect变成uv，就程颐该参数
        //
        GetUIVertex(colorTemp, originPos, vertPos, uvCenter, rect2UVRatio,out UIVertex origin);
        vh.AddVert(origin);
        //
        //
        float radian = (2 * Mathf.PI) / segements;//每个三角形分到的度数
        float radius = width * 0.5f;//半径
        int realSegments = (int)(segements * showPercent);//要显示的片元数
        int vertexCount = realSegments + 1;
        float curRadian = 0;
        for (int i = 0; i < segements + 1; i++)
        {
            bool isShow = i < vertexCount;
            GetUIVertexColor( isShow, out colorTemp);
            GetTrianglePointPos(radius, curRadian,out Vector2 posTermp);          
            GetUIVertex(colorTemp, posTermp + originPos, posTermp, uvCenter, rect2UVRatio,out UIVertex vertexTemp);
            //
            vh.AddVert(vertexTemp);
            _vertexList.Add(posTermp + originPos);
            //
            curRadian += radian;
        }
    }

    void GetOriginPos(float width, float height, out Vector2 originPos)
    {
        originPos = new Vector2((0.5f - rectTransform.pivot.x) * width, (0.5f - rectTransform.pivot.y) * height);//顶点坐标
    }
    void GetRectWH(RectTransform rectTransform, out float width, out float height)
    {
        width = rectTransform.rect.width;
        height = rectTransform.rect.height;
    }

    /// <summary>
    /// 三角形不在半径上的点的位置
    /// </summary>
    /// <param name="radius">半径</param>
    /// <param name="curRadian">度数</param>
    /// <returns></returns>
    void GetTrianglePointPos(float radius, float curRadian,out Vector2 pos)
    {
        float x = Mathf.Cos(curRadian) * radius;
        float y = Mathf.Sin(curRadian) * radius;
        pos= new Vector2(x, y);
    }

    /// <summary>
    /// 得到uv的宽高
    /// </summary>
    /// <param name="overrideSprite"></param>
    /// <param name="uvWidth"></param>
    /// <param name="uvHeight"></param>
    void GetSpriteWHC(Sprite overrideSprite, out float uvWidth, out float uvHeight,out Vector2 uvCenter)
    {
        Vector4 uv = overrideSprite != null ? DataUtility.GetOuterUV(overrideSprite) : Vector4.zero;
        uvWidth = uv.z - uv.x;//坐标系上矩形的最近点，最远点的坐标
        uvHeight = uv.w - uv.y;
        uvCenter = new Vector2(uvWidth * 0.5f, uvHeight * 0.5f);
    }

    /// <summary>
    /// 根据比例渲染
    /// </summary>
    /// <param name="showPercent"></param>
    /// <returns></returns>
    private Color32 GetOriginColor(float showPercent)
    {
        Color32 colorTemp = (Color.white - MASK_COLOR) * showPercent;
        return new Color32(
            (byte)(MASK_COLOR.r + colorTemp.r),
            (byte)(MASK_COLOR.g + colorTemp.g),
            (byte)(MASK_COLOR.b + colorTemp.b),
            255);
    }

    /// <summary>
    /// 顺时针生成三角片元
    /// </summary>
    /// <param name="vh">给GPU的数据类</param>
    /// <param name="realSegements">片元数</param>
    private void AddTriangle(VertexHelper vh, int realSegements)
    {
        int id = 1;//片元id
        for (int i = 0; i < realSegements; i++)
        {
            vh.AddTriangle(id, 0, id + 1);//102,203,304
            id++;
        }
    }
    void GetUIVertexColor(bool isShow, out Color32 colorTemp)
    {
        if (isShow)
        {
            colorTemp = color;//原来的颜色
        }
        else
        {
            colorTemp = MASK_COLOR;//遮罩色
        }

    }
    void  GetUIVertex(Color32 col, Vector3 pos, Vector2 uvPos, Vector2 uvCenter, Vector2 uvScale, out UIVertex uiVertex)
    {
        uiVertex = new UIVertex();
        uiVertex.color = col;
        uiVertex.position = pos;
        uiVertex.uv0 = new Vector2(uvPos.x * uvScale.x + uvCenter.x, uvPos.y * uvScale.y + uvCenter.y);//UV坐标
    }

    /// <summary>
    /// 可以点击
    /// </summary>
    /// <param name="localPoint">点击位置</param>
    /// <returns></returns>
    private bool IsValid(Vector2 localPoint)
    {
        return GetCrossPointNum(localPoint, _vertexList) % 2 == 1;
    }
    /// <summary>
    /// 点击点与外围相交点的数量
    /// </summary>
    /// <param name="localPoint">点击的位置</param>
    /// <param name="vertexList">外围的点列表</param>
    /// <returns></returns>
    private int GetCrossPointNum(Vector2 localPoint, List<Vector3> vertexList)
    {
        int count = 0;
        Vector3 vert1 = Vector3.zero;//前一个点
        Vector3 vert2 = Vector3.zero;//后一个点
        int vertCount = vertexList.Count;

        for (int i = 0; i < vertCount; i++)
        {
            vert1 = vertexList[i];
            vert2 = vertexList[(i + 1) % vertCount];//最后为其实，首尾相连

            if (IsYInRang(localPoint, vert1, vert2))
            {
                if (localPoint.x < GetX(vert1, vert2, localPoint.y))
                {
                    count++;
                }
            }
        }

        return count;
    }
   /// <summary>
   /// y在range1、range2之间
   /// </summary>
   /// <param name="localPoint"></param>
   /// <param name="v1"></param>
   /// <param name="v2"></param>
   /// <returns></returns>

    private bool IsYInRang(Vector2 localPoint, Vector3 v1, Vector3 v2)
    {
        //if (v1.y > v2.y)
        //{
        //    return localPoint.y < v1.y && localPoint.y > v2.y;
        //}
        //else
        //{
        //    return localPoint.y < v2.y && localPoint.y > v1.y;
        //}
        if (v1.y > v2.y)
        {
            return IsYInRangeFunc(localPoint.y, v1.y, v2.y);
        }
        else
        {
            return IsYInRangeFunc(localPoint.y, v2.y, v1.y);
        }
    }

    private bool IsYInRangeFunc(float y, float y1, float y2)
    {
        return y < y1 && y > y2;
    }

    /// <summary>
    /// 未知点的y求未知点的x
    /// </summary>
    /// <param name="vert1">已知点1</param>
    /// <param name="vert2">已知点2</param>
    /// <param name="y">未知点的y</param>
    /// <returns>未知点的x</returns>
    private float GetX(Vector3 vert1, Vector3 vert2, float y)
    {
        float k = (vert1.y - vert2.y) / (vert1.x - vert2.x);//斜率
        //k=( (vert1.y - y) / (vert1.x - x))  =>  x=(y-vert1.y)/k+vert1.x
        return   (y - vert1.y) / k + vert1.x;
    }
    #endregion

}
