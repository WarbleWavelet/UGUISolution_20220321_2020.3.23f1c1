﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;

namespace XCharts
{
    public class CoordinateChart : BaseChart
    {
        private static readonly string s_DefaultAxisY = "axis_y";
        private static readonly string s_DefaultAxisX = "axis_x";
        private static readonly string s_DefaultDataZoom = "datazoom";

        [SerializeField] protected Coordinate m_Coordinate = Coordinate.defaultCoordinate;
        [SerializeField] protected List<XAxis> m_XAxises = new List<XAxis>();
        [SerializeField] protected List<YAxis> m_YAxises = new List<YAxis>();
        [SerializeField] protected DataZoom m_DataZoom = DataZoom.defaultDataZoom;

        private bool m_DataZoomDrag;
        private bool m_DataZoomStartDrag;
        private bool m_DataZoomEndDrag;
        private float m_DataZoomLastStartIndex;
        private float m_DataZoomLastEndIndex;

        private List<XAxis> m_CheckXAxises = new List<XAxis>();
        private List<YAxis> m_CheckYAxises = new List<YAxis>();
        private Coordinate m_CheckCoordinate = Coordinate.defaultCoordinate;

        public float coordinateX { get { return m_Coordinate.left; } }
        public float coordinateY { get { return m_Coordinate.bottom; } }
        public float coordinateWid { get { return chartWidth - m_Coordinate.left - m_Coordinate.right; } }
        public float coordinateHig { get { return chartHeight - m_Coordinate.top - m_Coordinate.bottom; } }
        public List<XAxis> xAxises { get { return m_XAxises; } }
        public List<YAxis> yAxises { get { return m_YAxises; } }

        /// <summary>
        /// Remove all data from series,legend and axis.
        /// It just emptying all of serie's data without emptying the list of series.
        /// </summary>
        public override void ClearData()
        {
            base.ClearData();
            ClearAxisData();
        }

        /// <summary>
        /// Remove all data from series,legend and axis.
        /// The series list is also cleared.
        /// </summary>
        public override void RemoveData()
        {
            base.RemoveData();
            ClearAxisData();
        }

        /// <summary>
        /// Remove all data of axises.
        /// </summary>
        public void ClearAxisData()
        {
            foreach (var item in m_XAxises) item.data.Clear();
            foreach (var item in m_YAxises) item.data.Clear();
        }

        /// <summary>
        /// Add a category data to xAxis.
        /// </summary>
        /// <param name="category">the category data</param>
        /// <param name="xAxisIndex">which xAxis should category add to</param>
        public void AddXAxisData(string category, int xAxisIndex = 0)
        {
            m_XAxises[xAxisIndex].AddData(category, m_MaxCacheDataNumber);
            OnXAxisChanged();
        }

        /// <summary>
        /// Add a category data to yAxis.
        /// </summary>
        /// <param name="category">the category data</param>
        /// <param name="yAxisIndex">which yAxis should category add to</param>
        public void AddYAxisData(string category, int yAxisIndex = 0)
        {
            m_YAxises[yAxisIndex].AddData(category, m_MaxCacheDataNumber);
            OnYAxisChanged();
        }

        /// <summary>
        /// Whether is Cartesian coordinates, reutrn true when all the show axis is `Value` type.
        /// </summary>
        /// <returns></returns>
        public bool IsCartesian()
        {
            foreach (var axis in m_XAxises)
            {
                if (axis.show && !axis.IsValue()) return false;
            }
            foreach (var axis in m_YAxises)
            {
                if (axis.show && !axis.IsValue()) return false;
            }
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            InitDefaultAxises();
            CheckMinMaxValue();
            InitDataZoom();
            InitAxisX();
            InitAxisY();
            m_Tooltip.UpdateToTop();
        }

        protected override void Update()
        {
            base.Update();
            CheckYAxis();
            CheckXAxis();
            CheckMinMaxValue();
            CheckCoordinate();
            CheckDataZoom();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            m_Coordinate = Coordinate.defaultCoordinate;
            m_XAxises.Clear();
            m_YAxises.Clear();
            Awake();
        }
#endif

        protected override void DrawChart(VertexHelper vh)
        {
            base.DrawChart(vh);
            DrawCoordinate(vh);
            DrawDataZoom(vh);
        }

        protected override void CheckTootipArea(Vector2 local)
        {
            if (local.x < coordinateX - 1 || local.x > coordinateX + coordinateWid + 1 ||
                local.y < coordinateY - 1 || local.y > coordinateY + coordinateHig + 1)
            {
                m_Tooltip.ClearValue();
                RefreshTooltip();
            }
            else
            {
                var isCartesian = IsCartesian();
                for (int i = 0; i < m_XAxises.Count; i++)
                {
                    var xAxis = m_XAxises[i];
                    var yAxis = m_YAxises[i];
                    if (!xAxis.show && !yAxis.show) continue;
                    if (isCartesian && xAxis.show && yAxis.show)
                    {
                        var yRate = (yAxis.maxValue - yAxis.minValue) / coordinateHig;
                        var xRate = (xAxis.maxValue - xAxis.minValue) / coordinateWid;
                        var yValue = yRate * (local.y - coordinateY - yAxis.zeroYOffset);
                        if (yAxis.minValue > 0) yValue += yAxis.minValue;
                        m_Tooltip.yValues[i] = yValue;
                        var xValue = xRate * (local.x - coordinateX - xAxis.zeroXOffset);
                        if (xAxis.minValue > 0) xValue += xAxis.minValue;
                        m_Tooltip.xValues[i] = xValue;

                        for (int j = 0; j < m_Series.Count; j++)
                        {
                            var serie = m_Series.GetSerie(j);
                            serie.selected = false;
                            for (int n = 0; n < serie.xData.Count; n++)
                            {
                                var xdata = serie.xData[n];
                                var ydata = serie.yData[n];
                                var serieData = serie.GetSerieData(n);
                                var symbolSize = serie.symbol.GetSize(serieData == null ? null : serieData.data);
                                if (Mathf.Abs(xValue - xdata) / xRate < symbolSize
                                    && Mathf.Abs(yValue - ydata) / yRate < symbolSize)
                                {
                                    m_Tooltip.dataIndex[i] = n;
                                    serie.selected = true;
                                }
                            }
                        }
                    }
                    else if (xAxis.IsCategory())
                    {
                        var value = (yAxis.maxValue - yAxis.minValue) * (local.y - coordinateY - yAxis.zeroYOffset) / coordinateHig;
                        if (yAxis.minValue > 0) value += yAxis.minValue;
                        m_Tooltip.yValues[i] = value;
                        for (int j = 0; j < xAxis.GetDataNumber(m_DataZoom); j++)
                        {
                            float splitWid = xAxis.GetDataWidth(coordinateWid, m_DataZoom);
                            float pX = coordinateX + j * splitWid;
                            if ((xAxis.boundaryGap && (local.x > pX && local.x <= pX + splitWid)) ||
                                (!xAxis.boundaryGap && (local.x > pX - splitWid / 2 && local.x <= pX + splitWid / 2)))
                            {
                                m_Tooltip.xValues[i] = j;
                                m_Tooltip.dataIndex[i] = j;
                                break;
                            }
                        }
                    }
                    else if (yAxis.IsCategory())
                    {
                        var value = (xAxis.maxValue - xAxis.minValue) * (local.x - coordinateX - xAxis.zeroXOffset) / coordinateWid;
                        if (xAxis.minValue > 0) value += xAxis.minValue;
                        m_Tooltip.xValues[i] = value;
                        for (int j = 0; j < yAxis.GetDataNumber(m_DataZoom); j++)
                        {
                            float splitWid = yAxis.GetDataWidth(coordinateHig, m_DataZoom);
                            float pY = coordinateY + j * splitWid;
                            if ((yAxis.boundaryGap && (local.y > pY && local.y <= pY + splitWid)) ||
                                (!yAxis.boundaryGap && (local.y > pY - splitWid / 2 && local.y <= pY + splitWid / 2)))
                            {
                                m_Tooltip.yValues[i] = j;
                                m_Tooltip.dataIndex[i] = j;
                                break;
                            }
                        }
                    }
                }
            }
            if (m_Tooltip.IsSelected())
            {
                m_Tooltip.UpdateContentPos(new Vector2(local.x + 18, local.y - 25));
                RefreshTooltip();
                if (m_Tooltip.IsDataIndexChanged() || m_Tooltip.type == Tooltip.Type.Corss)
                {
                    m_Tooltip.UpdateLastDataIndex();
                    RefreshChart();
                }
            }
            else
            {
                m_Tooltip.SetActive(false);
            }
        }

        private StringBuilder sb = new StringBuilder(100);
        protected override void RefreshTooltip()
        {
            base.RefreshTooltip();
            int index;
            Axis tempAxis;
            bool isCartesian = IsCartesian();
            if (isCartesian)
            {
                index = m_Tooltip.dataIndex[0];
                tempAxis = m_XAxises[0];
            }
            else if (m_XAxises[0].type == Axis.AxisType.Value)
            {
                index = (int)m_Tooltip.yValues[0];
                tempAxis = m_YAxises[0];
            }
            else
            {
                index = (int)m_Tooltip.xValues[0];
                tempAxis = m_XAxises[0];
            }
            if (index < 0)
            {
                m_Tooltip.SetActive(false);
                return;
            }

            sb.Length = 0;

            if (!isCartesian)
            {
                sb.Append(tempAxis.GetData(index, m_DataZoom));
            }
            for (int i = 0; i < m_Series.Count; i++)
            {
                var serie = m_Series.GetSerie(i);
                if (serie.show)
                {
                    string key = serie.name;
                    float xValue, yValue;
                    serie.GetXYData(index, m_DataZoom, out xValue, out yValue);
                    if (isCartesian)
                    {
                        if (serie.selected)
                        {
                            sb.Append(key).Append(!string.IsNullOrEmpty(key) ? " : " : "");
                            sb.Append("[").Append(ChartCached.FloatToStr(xValue)).Append(",")
                                .Append(ChartCached.FloatToStr(yValue)).Append("]\n");
                        }
                    }
                    else
                    {
                        sb.Append("\n")
                            .Append("<color=").Append(m_ThemeInfo.GetColorStr(i)).Append(">● </color>")
                            .Append(key).Append(!string.IsNullOrEmpty(key) ? " : " : "")
                            .Append(ChartCached.FloatToStr(yValue));
                    }
                }
            }
            m_Tooltip.UpdateContentText(sb.ToString().Trim());

            var pos = m_Tooltip.GetContentPos();
            if (pos.x + m_Tooltip.width > chartWidth)
            {
                pos.x = chartWidth - m_Tooltip.width;
            }
            if (pos.y - m_Tooltip.height < 0)
            {
                pos.y = m_Tooltip.height;
            }
            m_Tooltip.UpdateContentPos(pos);
            m_Tooltip.SetActive(true);

            for (int i = 0; i < m_XAxises.Count; i++)
            {
                UpdateAxisTooltipLabel(i, m_XAxises[i]);
            }
            for (int i = 0; i < m_YAxises.Count; i++)
            {
                UpdateAxisTooltipLabel(i, m_YAxises[i]);
            }
        }

        private void UpdateAxisTooltipLabel(int axisIndex, Axis axis)
        {
            var showTooltipLabel = axis.show && m_Tooltip.type == Tooltip.Type.Corss;
            axis.SetTooltipLabelActive(showTooltipLabel);
            if (!showTooltipLabel) return;
            string labelText = "";
            Vector2 labelPos = Vector2.zero;
            if (axis is XAxis)
            {
                var posY = axisIndex > 0 ? coordinateY + coordinateHig : coordinateY;
                var diff = axisIndex > 0 ? -axis.axisLabel.fontSize - axis.axisLabel.margin - 3.5f : axis.axisLabel.margin / 2 + 1;
                if (axis.IsValue())
                {
                    labelText = ChartCached.FloatToStr(m_Tooltip.xValues[axisIndex], 2);
                    labelPos = new Vector2(m_Tooltip.pointerPos.x, posY - diff);
                }
                else
                {
                    labelText = axis.GetData((int)m_Tooltip.xValues[axisIndex], m_DataZoom);
                    float splitWidth = axis.GetSplitWidth(coordinateWid, m_DataZoom);
                    int index = (int)m_Tooltip.xValues[axisIndex];
                    float px = coordinateX + index * splitWidth + (axis.boundaryGap ? splitWidth / 2 : 0) + 0.5f;
                    labelPos = new Vector2(px, posY - diff);
                }
            }
            else if (axis is YAxis)
            {
                var posX = axisIndex > 0 ? coordinateX + coordinateWid : coordinateX;
                var diff = axisIndex > 0 ? -axis.axisLabel.margin + 3 : axis.axisLabel.margin - 3;
                if (axis.IsValue())
                {
                    labelText = ChartCached.FloatToStr(m_Tooltip.yValues[axisIndex], 2);
                    labelPos = new Vector2(posX - diff, m_Tooltip.pointerPos.y);
                }
                else
                {
                    labelText = axis.GetData((int)m_Tooltip.yValues[axisIndex], m_DataZoom);
                    float splitWidth = axis.GetSplitWidth(coordinateHig, m_DataZoom);
                    int index = (int)m_Tooltip.yValues[axisIndex];
                    float py = coordinateY + index * splitWidth + (axis.boundaryGap ? splitWidth / 2 : 0);
                    labelPos = new Vector2(posX - diff, py);
                }
            }
            axis.UpdateTooptipLabelText(labelText);
            axis.UpdateTooltipLabelPos(labelPos);
        }

        protected override void OnThemeChanged()
        {
            base.OnThemeChanged();
            InitDataZoom();
            InitAxisX();
            InitAxisY();
        }

        private void InitDefaultAxises()
        {
            if (m_XAxises.Count <= 0)
            {
                var axis1 = XAxis.defaultXAxis;
                var axis2 = XAxis.defaultXAxis;
                axis1.show = true;
                axis2.show = false;
                m_XAxises.Add(axis1);
                m_XAxises.Add(axis2);
            }
            if (m_YAxises.Count <= 0)
            {
                var axis1 = YAxis.defaultYAxis;
                var axis2 = YAxis.defaultYAxis;
                axis1.show = true;
                axis1.splitNumber = 5;
                axis1.boundaryGap = false;
                axis2.show = false;
                m_YAxises.Add(axis1);
                m_YAxises.Add(axis2);
            }
        }

        private void InitAxisY()
        {
            ChartHelper.HideAllObject(gameObject, "split_y");//old name
            for (int i = 0; i < m_YAxises.Count; i++)
            {
                InitYAxis(i, m_YAxises[i]);
            }
        }

        private void InitYAxis(int yAxisIndex, YAxis yAxis)
        {
            yAxis.axisLabelTextList.Clear();
            float labelWidth = yAxis.GetScaleWidth(coordinateHig, m_DataZoom);
            string objName = yAxisIndex > 0 ? s_DefaultAxisY + "2" : s_DefaultAxisY;

            var axisObj = ChartHelper.AddObject(objName, transform, chartAnchorMin,
                chartAnchorMax, chartPivot, new Vector2(chartWidth, chartHeight));
            axisObj.transform.localPosition = Vector3.zero;
            axisObj.SetActive(yAxis.show && yAxis.axisLabel.show);
            ChartHelper.HideAllObject(axisObj, objName);

            var labelColor = yAxis.axisLabel.color == Color.clear ?
                (Color)m_ThemeInfo.axisTextColor :
                yAxis.axisLabel.color;
            for (int i = 0; i < yAxis.GetSplitNumber(m_DataZoom); i++)
            {
                Text txt;
                bool inside = yAxis.axisLabel.inside;
                if ((inside && yAxisIndex == 0) || (!inside && yAxisIndex == 1))
                {
                    txt = ChartHelper.AddTextObject(objName + i, axisObj.transform,
                        m_ThemeInfo.font, labelColor, TextAnchor.MiddleLeft, Vector2.zero,
                        Vector2.zero, new Vector2(0, 0.5f), new Vector2(m_Coordinate.left, 20),
                        yAxis.axisLabel.fontSize, yAxis.axisLabel.rotate, yAxis.axisLabel.fontStyle);
                }
                else
                {
                    txt = ChartHelper.AddTextObject(objName + i, axisObj.transform,
                        m_ThemeInfo.font, labelColor, TextAnchor.MiddleRight, Vector2.zero,
                        Vector2.zero, new Vector2(1, 0.5f), new Vector2(m_Coordinate.left, 20),
                        yAxis.axisLabel.fontSize, yAxis.axisLabel.rotate, yAxis.axisLabel.fontStyle);
                }

                txt.transform.localPosition = GetLabelYPosition(labelWidth, i, yAxisIndex, yAxis);
                txt.text = yAxis.GetLabelName(i, yAxis.minValue, yAxis.maxValue, m_DataZoom);
                txt.gameObject.SetActive(yAxis.show &&
                    (yAxis.axisLabel.interval == 0 || i % (yAxis.axisLabel.interval + 1) == 0));
                yAxis.axisLabelTextList.Add(txt);
            }
            if (yAxis.axisName.show)
            {
                var color = yAxis.axisName.color == Color.clear ? (Color)m_ThemeInfo.axisTextColor :
                    yAxis.axisName.color;
                var fontSize = yAxis.axisName.fontSize;
                var gap = yAxis.axisName.gap;
                Text axisName;
                switch (yAxis.axisName.location)
                {
                    case AxisName.Location.Start:
                        axisName = ChartHelper.AddTextObject(objName + "_name", axisObj.transform,
                             m_ThemeInfo.font, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                             new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100, 20), fontSize,
                             yAxis.axisName.rotate, yAxis.axisName.fontStyle);
                        axisName.transform.localPosition = yAxisIndex > 0 ?
                            new Vector2(coordinateX + coordinateWid, coordinateY - gap) :
                            new Vector2(coordinateX, coordinateY - gap);
                        break;
                    case AxisName.Location.Middle:
                        axisName = ChartHelper.AddTextObject(objName + "_name", axisObj.transform,
                            m_ThemeInfo.font, color, TextAnchor.MiddleRight, new Vector2(1, 0.5f),
                            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(100, 20), fontSize,
                            yAxis.axisName.rotate, yAxis.axisName.fontStyle);
                        axisName.transform.localPosition = yAxisIndex > 0 ?
                        new Vector2(coordinateX + coordinateWid - gap, coordinateY + coordinateHig / 2) :
                        new Vector2(coordinateX - gap, coordinateY + coordinateHig / 2);
                        break;
                    case AxisName.Location.End:
                        axisName = ChartHelper.AddTextObject(objName + "_name", axisObj.transform,
                             m_ThemeInfo.font, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                             new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100, 20), fontSize,
                             yAxis.axisName.rotate, yAxis.axisName.fontStyle);
                        axisName.transform.localPosition = yAxisIndex > 0 ?
                            new Vector2(coordinateX + coordinateWid, coordinateY + coordinateHig + gap) :
                            new Vector2(coordinateX, coordinateY + coordinateHig + gap);
                        break;
                }
            }
            //init tooltip label
            if (m_Tooltip.gameObject)
            {
                Vector2 privot = yAxisIndex > 0 ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
                var labelParent = m_Tooltip.gameObject.transform;
                GameObject labelObj = ChartHelper.AddTooltipLabel(objName + "_label", labelParent, m_ThemeInfo.font, privot);
                yAxis.SetTooltipLabel(labelObj);
                yAxis.SetTooltipLabelColor(m_ThemeInfo.tooltipBackgroundColor, m_ThemeInfo.tooltipTextColor);
                yAxis.SetTooltipLabelActive(yAxis.show && m_Tooltip.show && m_Tooltip.type == Tooltip.Type.Corss);
            }
        }

        private void InitAxisX()
        {
            ChartHelper.HideAllObject(gameObject, "split_x");//old name
            for (int i = 0; i < m_XAxises.Count; i++)
            {
                InitXAxis(i, m_XAxises[i]);
            }
        }

        private void InitXAxis(int xAxisIndex, XAxis xAxis)
        {
            xAxis.minValue = 0;
            xAxis.maxValue = 100;
            xAxis.axisLabelTextList.Clear();
            float labelWidth = xAxis.GetScaleWidth(coordinateWid, m_DataZoom);
            string objName = xAxisIndex > 0 ? s_DefaultAxisX + "2" : s_DefaultAxisX;
            var axisObj = ChartHelper.AddObject(objName, transform, chartAnchorMin,
                chartAnchorMax, chartPivot, new Vector2(chartWidth, chartHeight));
            axisObj.transform.localPosition = Vector3.zero;
            axisObj.SetActive(xAxis.show && xAxis.axisLabel.show);
            ChartHelper.HideAllObject(axisObj, objName);
            var labelColor = xAxis.axisLabel.color == Color.clear ?
                (Color)m_ThemeInfo.axisTextColor :
                xAxis.axisLabel.color;
            for (int i = 0; i < xAxis.GetSplitNumber(m_DataZoom); i++)
            {
                bool inside = xAxis.axisLabel.inside;
                Text txt = ChartHelper.AddTextObject(objName + i, axisObj.transform,
                    m_ThemeInfo.font, labelColor, TextAnchor.MiddleCenter, new Vector2(0, 1),
                    new Vector2(0, 1), new Vector2(1, 0.5f), new Vector2(labelWidth, 20),
                    xAxis.axisLabel.fontSize, xAxis.axisLabel.rotate, xAxis.axisLabel.fontStyle);

                txt.transform.localPosition = GetLabelXPosition(labelWidth, i, xAxisIndex, xAxis);
                txt.text = xAxis.GetLabelName(i, xAxis.minValue, xAxis.maxValue, m_DataZoom);
                txt.gameObject.SetActive(xAxis.show &&
                    (xAxis.axisLabel.interval == 0 || i % (xAxis.axisLabel.interval + 1) == 0));
                xAxis.axisLabelTextList.Add(txt);
            }
            if (xAxis.axisName.show)
            {
                var color = xAxis.axisName.color == Color.clear ? (Color)m_ThemeInfo.axisTextColor :
                    xAxis.axisName.color;
                var fontSize = xAxis.axisName.fontSize;
                var gap = xAxis.axisName.gap;
                Text axisName;
                switch (xAxis.axisName.location)
                {
                    case AxisName.Location.Start:
                        axisName = ChartHelper.AddTextObject(objName + "_name", axisObj.transform,
                            m_ThemeInfo.font, color, TextAnchor.MiddleRight, new Vector2(1, 0.5f),
                            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(100, 20), fontSize,
                            xAxis.axisName.rotate, xAxis.axisName.fontStyle);
                        axisName.transform.localPosition = xAxisIndex > 0 ?
                            new Vector2(coordinateX - gap, coordinateY + coordinateHig) :
                            new Vector2(coordinateX - gap, coordinateY);
                        break;
                    case AxisName.Location.Middle:
                        axisName = ChartHelper.AddTextObject(objName + "_name", axisObj.transform,
                             m_ThemeInfo.font, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                             new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100, 20), fontSize,
                             xAxis.axisName.rotate, xAxis.axisName.fontStyle);
                        axisName.transform.localPosition = xAxisIndex > 0 ?
                            new Vector2(coordinateX + coordinateWid / 2, coordinateY + coordinateHig - gap) :
                            new Vector2(coordinateX + coordinateWid / 2, coordinateY - gap);
                        break;
                    case AxisName.Location.End:
                        axisName = ChartHelper.AddTextObject(objName + "_name", axisObj.transform,
                             m_ThemeInfo.font, color, TextAnchor.MiddleLeft, new Vector2(0, 0.5f),
                             new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(100, 20), fontSize,
                             xAxis.axisName.rotate, xAxis.axisName.fontStyle);
                        axisName.transform.localPosition = xAxisIndex > 0 ?
                            new Vector2(coordinateX + coordinateWid + gap, coordinateY + coordinateHig) :
                            new Vector2(coordinateX + coordinateWid + gap, coordinateY);
                        break;
                }
            }
            if (m_Tooltip.gameObject)
            {
                Vector2 privot = xAxisIndex > 0 ? new Vector2(0.5f, 1) : new Vector2(0.5f, 1);
                var labelParent = m_Tooltip.gameObject.transform;
                GameObject labelObj = ChartHelper.AddTooltipLabel(objName + "_label", labelParent, m_ThemeInfo.font, privot);
                xAxis.SetTooltipLabel(labelObj);
                xAxis.SetTooltipLabelColor(m_ThemeInfo.tooltipBackgroundColor, m_ThemeInfo.tooltipTextColor);
                xAxis.SetTooltipLabelActive(xAxis.show && m_Tooltip.show && m_Tooltip.type == Tooltip.Type.Corss);
            }
        }

        private void InitDataZoom()
        {
            var dataZoomObject = ChartHelper.AddObject(s_DefaultDataZoom, transform, chartAnchorMin,
                chartAnchorMax, chartPivot, new Vector2(chartWidth, chartHeight));
            dataZoomObject.transform.localPosition = Vector3.zero;
            ChartHelper.HideAllObject(dataZoomObject, s_DefaultDataZoom);
            m_DataZoom.startLabel = ChartHelper.AddTextObject(s_DefaultDataZoom + "start",
                dataZoomObject.transform, m_ThemeInfo.font, m_ThemeInfo.dataZoomTextColor, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.zero, new Vector2(1, 0.5f), new Vector2(200, 20));
            m_DataZoom.endLabel = ChartHelper.AddTextObject(s_DefaultDataZoom + "end",
                dataZoomObject.transform, m_ThemeInfo.font, m_ThemeInfo.dataZoomTextColor, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.zero, new Vector2(0, 0.5f), new Vector2(200, 20));
            m_DataZoom.SetLabelActive(false);
            raycastTarget = m_DataZoom.show;
            var xAxis = m_XAxises[m_DataZoom.xAxisIndex];
            if (xAxis != null)
            {
                xAxis.UpdateFilterData(m_DataZoom);
            }
            if (m_Series != null)
            {
                m_Series.UpdateFilterData(m_DataZoom);
            }
        }

        private Vector3 GetLabelYPosition(float scaleWid, int i, int yAxisIndex, YAxis yAxis)
        {
            var startX = yAxisIndex == 0 ? coordinateX : coordinateX + coordinateWid;
            var posX = 0f;
            var inside = yAxis.axisLabel.inside;
            if ((inside && yAxisIndex == 0) || (!inside && yAxisIndex == 1))
            {
                posX = startX + yAxis.axisLabel.margin;
            }
            else
            {
                posX = startX - yAxis.axisLabel.margin;
            }
            if (yAxis.boundaryGap)
            {
                return new Vector3(posX, coordinateY + (i + 0.5f) * scaleWid, 0);
            }
            else
            {
                return new Vector3(posX, coordinateY + i * scaleWid, 0);
            }
        }

        private Vector3 GetLabelXPosition(float scaleWid, int i, int xAxisIndex, XAxis xAxis)
        {
            var startY = xAxisIndex == 0 ? coordinateY : coordinateY + coordinateHig;
            var posY = 0f;
            var inside = xAxis.axisLabel.inside;
            if ((inside && xAxisIndex == 0) || (!inside && xAxisIndex == 1))
            {
                posY = startY + xAxis.axisLabel.margin + xAxis.axisLabel.fontSize / 2;
            }
            else
            {
                posY = startY - xAxis.axisLabel.margin - xAxis.axisLabel.fontSize / 2;
            }
            if (xAxis.boundaryGap)
            {
                return new Vector3(coordinateX + (i + 1) * scaleWid, posY);
            }
            else
            {
                return new Vector3(coordinateX + (i + 1 - 0.5f) * scaleWid, posY);
            }
        }

        private void CheckCoordinate()
        {
            if (m_CheckCoordinate != m_Coordinate)
            {
                m_CheckCoordinate.Copy(m_Coordinate);
                OnCoordinateChanged();
            }
        }

        private void CheckYAxis()
        {
            if (!ChartHelper.IsValueEqualsList<YAxis>(m_CheckYAxises, m_YAxises))
            {
                m_CheckYAxises.Clear();
                foreach (var axis in m_YAxises) m_CheckYAxises.Add(axis.Clone());
                OnYAxisChanged();
            }
        }

        private void CheckXAxis()
        {
            if (!ChartHelper.IsValueEqualsList<XAxis>(m_CheckXAxises, m_XAxises))
            {
                m_CheckXAxises.Clear();
                foreach (var axis in m_XAxises) m_CheckXAxises.Add(axis.Clone());
                OnXAxisChanged();
            }
        }

        private void CheckMinMaxValue()
        {
            if (m_XAxises == null || m_YAxises == null) return;
            for (int i = 0; i < m_XAxises.Count; i++)
            {
                UpdateAxisMinMaxValue(i, m_XAxises[i]);
            }
            for (int i = 0; i < m_YAxises.Count; i++)
            {
                UpdateAxisMinMaxValue(i, m_YAxises[i]);
            }
        }

        private void UpdateAxisMinMaxValue(int axisIndex, Axis axis)
        {
            axis.minValue = 0;
            axis.maxValue = 100;
            if (axis.IsCategory()) return;

            int tempMinValue = 0;
            int tempMaxValue = 100;
            if (m_XAxises[axisIndex].IsValue() && m_YAxises[axisIndex].IsValue())
            {
                if (axis is XAxis)
                {
                    m_Series.GetXMinMaxValue(m_DataZoom, axisIndex, out tempMinValue, out tempMaxValue);
                }
                else
                {
                    m_Series.GetYMinMaxValue(m_DataZoom, axisIndex, out tempMinValue, out tempMaxValue);
                }
            }
            else
            {
                m_Series.GetYMinMaxValue(m_DataZoom, axisIndex, out tempMinValue, out tempMaxValue);
            }
            axis.AdjustMinMaxValue(ref tempMinValue, ref tempMaxValue);

            if (tempMinValue != axis.minValue || tempMaxValue != axis.maxValue)
            {
                axis.minValue = tempMinValue;
                axis.maxValue = tempMaxValue;
                axis.zeroXOffset = 0;
                axis.zeroYOffset = 0;
                if (axis is XAxis && axis.IsValue())
                {
                    axis.zeroXOffset = axis.minValue > 0 ? 0 :
                        axis.maxValue < 0 ? coordinateWid :
                        Mathf.Abs(axis.minValue) * (coordinateWid / (Mathf.Abs(axis.minValue) + Mathf.Abs(axis.maxValue)));
                }
                if (axis is YAxis && axis.IsValue())
                {
                    axis.zeroYOffset = axis.minValue > 0 ? 0 :
                        axis.maxValue < 0 ? coordinateHig :
                        Mathf.Abs(axis.minValue) * (coordinateHig / (Mathf.Abs(axis.minValue) + Mathf.Abs(axis.maxValue)));
                }
                axis.UpdateLabelText(m_DataZoom);
                RefreshChart();
            }
        }

        protected virtual void OnCoordinateChanged()
        {
            InitAxisX();
            InitAxisY();
        }

        protected virtual void OnYAxisChanged()
        {
            InitAxisY();
        }

        protected virtual void OnXAxisChanged()
        {
            InitAxisX();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            InitAxisX();
            InitAxisY();
        }

        private void DrawCoordinate(VertexHelper vh)
        {
            for (int i = 0; i < m_XAxises.Count; i++)
            {
                DrawXAxisTickAndSplit(vh, i, m_XAxises[i]);
            }
            for (int i = 0; i < m_YAxises.Count; i++)
            {
                DrawYAxisTickAndSplit(vh, i, m_YAxises[i]);
            }
            for (int i = 0; i < m_XAxises.Count; i++)
            {
                DrawXAxisLine(vh, i, m_XAxises[i]);
            }
            for (int i = 0; i < m_YAxises.Count; i++)
            {
                DrawYAxisLine(vh, i, m_YAxises[i]);
            }
        }

        private void DrawYAxisTickAndSplit(VertexHelper vh, int yAxisIndex, YAxis yAxis)
        {
            if (yAxis.show)
            {
                var scaleWidth = yAxis.GetScaleWidth(coordinateHig, m_DataZoom);
                var size = yAxis.GetScaleNumber(m_DataZoom);
                for (int i = 0; i < size; i++)
                {
                    float pX = 0;
                    float pY = coordinateY + i * scaleWidth;
                    if (yAxis.boundaryGap && yAxis.axisTick.alignWithLabel)
                    {
                        pY -= scaleWidth / 2;
                    }
                    if (yAxis.splitArea.show && i < size - 1)
                    {
                        ChartHelper.DrawPolygon(vh, new Vector2(coordinateX, pY),
                            new Vector2(coordinateX + coordinateWid, pY),
                            new Vector2(coordinateX + coordinateWid, pY + scaleWidth),
                            new Vector2(coordinateX, pY + scaleWidth),
                            yAxis.splitArea.getColor(i));
                    }
                    if (yAxis.axisTick.show)
                    {
                        var startX = coordinateX + m_XAxises[yAxisIndex].zeroXOffset;
                        if (yAxis.IsValue() && yAxisIndex > 0) startX += coordinateWid;
                        bool inside = yAxis.axisTick.inside;
                        if ((inside && yAxisIndex == 0) || (!inside && yAxisIndex == 1))
                        {
                            pX += startX + yAxis.axisTick.length;
                        }
                        else
                        {
                            pX += startX - yAxis.axisTick.length;
                        }
                        ChartHelper.DrawLine(vh, new Vector3(startX, pY), new Vector3(pX, pY),
                            m_Coordinate.tickness, m_ThemeInfo.axisLineColor);
                    }
                    if (yAxis.showSplitLine)
                    {
                        DrawSplitLine(vh, true, yAxis.splitLineType, new Vector3(coordinateX, pY),
                            new Vector3(coordinateX + coordinateWid, pY), m_ThemeInfo.axisSplitLineColor);
                    }
                }
            }
        }

        private void DrawXAxisTickAndSplit(VertexHelper vh, int xAxisIndex, XAxis xAxis)
        {
            if (xAxis.show)
            {
                var scaleWidth = xAxis.GetScaleWidth(coordinateWid, m_DataZoom);
                var size = xAxis.GetScaleNumber(m_DataZoom);
                for (int i = 0; i < size; i++)
                {
                    float pX = coordinateX + i * scaleWidth;
                    float pY = 0;
                    if (xAxis.boundaryGap && xAxis.axisTick.alignWithLabel)
                    {
                        pX -= scaleWidth / 2;
                    }
                    if (xAxis.splitArea.show && i < size - 1)
                    {
                        ChartHelper.DrawPolygon(vh, new Vector2(pX, coordinateY),
                            new Vector2(pX, coordinateY + coordinateHig),
                            new Vector2(pX + scaleWidth, coordinateY + coordinateHig),
                            new Vector2(pX + scaleWidth, coordinateY),
                            xAxis.splitArea.getColor(i));
                    }
                    if (xAxis.axisTick.show)
                    {
                        var startY = coordinateY + m_YAxises[xAxisIndex].zeroYOffset;
                        if (xAxis.IsValue() && xAxisIndex > 0) startY += coordinateHig;
                        bool inside = xAxis.axisTick.inside;
                        if ((inside && xAxisIndex == 0) || (!inside && xAxisIndex == 1))
                        {
                            pY += startY + xAxis.axisTick.length;
                        }
                        else
                        {
                            pY += startY - xAxis.axisTick.length;
                        }
                        ChartHelper.DrawLine(vh, new Vector3(pX, startY), new Vector3(pX, pY),
                            m_Coordinate.tickness, m_ThemeInfo.axisLineColor);
                    }
                    if (xAxis.showSplitLine)
                    {
                        DrawSplitLine(vh, false, xAxis.splitLineType, new Vector3(pX, coordinateY),
                            new Vector3(pX, coordinateY + coordinateHig), m_ThemeInfo.axisSplitLineColor);
                    }
                }
            }
        }

        private void DrawXAxisLine(VertexHelper vh, int xAxisIndex, XAxis xAxis)
        {
            if (xAxis.show && xAxis.axisLine.show)
            {
                var lineY = coordinateY + (xAxis.axisLine.onZero ? m_YAxises[xAxisIndex].zeroYOffset : 0);
                if (xAxis.IsValue() && xAxisIndex > 0) lineY += coordinateHig;
                var left = new Vector3(coordinateX - m_Coordinate.tickness, lineY);
                var top = new Vector3(coordinateX + coordinateWid + m_Coordinate.tickness, lineY);
                ChartHelper.DrawLine(vh, left, top, m_Coordinate.tickness, m_ThemeInfo.axisLineColor);
                if (xAxis.axisLine.symbol)
                {
                    var axisLine = xAxis.axisLine;
                    top.x += xAxis.axisLine.symbolOffset;
                    var middle = new Vector3(top.x - axisLine.symbolHeight + axisLine.symbolDent, lineY);
                    left = new Vector3(top.x - axisLine.symbolHeight, lineY - axisLine.symbolWidth / 2);
                    var right = new Vector3(top.x - axisLine.symbolHeight, lineY + axisLine.symbolWidth / 2);
                    ChartHelper.DrawTriangle(vh, middle, top, left, m_ThemeInfo.axisLineColor);
                    ChartHelper.DrawTriangle(vh, middle, top, right, m_ThemeInfo.axisLineColor);
                }
            }
        }

        private void DrawYAxisLine(VertexHelper vh, int yAxisIndex, YAxis yAxis)
        {
            if (yAxis.show && yAxis.axisLine.show)
            {
                var lineX = coordinateX + (yAxis.axisLine.onZero ? m_XAxises[yAxisIndex].zeroXOffset : 0);
                if (yAxis.IsValue() && yAxisIndex > 0) lineX += coordinateWid;
                var top = new Vector3(lineX, coordinateY + coordinateHig + m_Coordinate.tickness);
                ChartHelper.DrawLine(vh, new Vector3(lineX, coordinateY - m_Coordinate.tickness),
                    top, m_Coordinate.tickness, m_ThemeInfo.axisLineColor);
                if (yAxis.axisLine.symbol)
                {
                    var axisLine = yAxis.axisLine;
                    top.y += yAxis.axisLine.symbolOffset;
                    var middle = new Vector3(lineX, top.y - axisLine.symbolHeight + axisLine.symbolDent);
                    var left = new Vector3(lineX - axisLine.symbolWidth / 2, top.y - axisLine.symbolHeight);
                    var right = new Vector3(lineX + axisLine.symbolWidth / 2, top.y - axisLine.symbolHeight);
                    ChartHelper.DrawTriangle(vh, middle, top, left, m_ThemeInfo.axisLineColor);
                    ChartHelper.DrawTriangle(vh, middle, top, right, m_ThemeInfo.axisLineColor);
                }
            }
        }

        private void DrawDataZoom(VertexHelper vh)
        {
            if (!m_DataZoom.show) return;
            var p1 = new Vector2(coordinateX, m_DataZoom.bottom);
            var p2 = new Vector2(coordinateX, m_DataZoom.bottom + m_DataZoom.height);
            var p3 = new Vector2(coordinateX + coordinateWid, m_DataZoom.bottom + m_DataZoom.height);
            var p4 = new Vector2(coordinateX + coordinateWid, m_DataZoom.bottom);
            ChartHelper.DrawLine(vh, p1, p2, m_Coordinate.tickness, m_ThemeInfo.dataZoomLineColor);
            ChartHelper.DrawLine(vh, p2, p3, m_Coordinate.tickness, m_ThemeInfo.dataZoomLineColor);
            ChartHelper.DrawLine(vh, p3, p4, m_Coordinate.tickness, m_ThemeInfo.dataZoomLineColor);
            ChartHelper.DrawLine(vh, p4, p1, m_Coordinate.tickness, m_ThemeInfo.dataZoomLineColor);
            if (m_DataZoom.showDataShadow && m_Series.Count > 0)
            {
                Serie serie = m_Series.series[0];
                Axis axis = yAxises[0];
                float scaleWid = coordinateWid / (serie.yData.Count - 1);
                Vector3 lp = Vector3.zero;
                Vector3 np = Vector3.zero;
                int minValue = 0;
                int maxValue = 100;
                m_Series.GetYMinMaxValue(null, 0, out minValue, out maxValue);
                axis.AdjustMinMaxValue(ref minValue, ref maxValue);
                if (minValue > 0 && maxValue > 0) minValue = 0;
                for (int i = 0; i < serie.yData.Count; i++)
                {
                    float value = serie.yData[i];
                    float pX = coordinateX + i * scaleWid;
                    float dataHig = value / (maxValue - minValue) * m_DataZoom.height;
                    np = new Vector3(pX, m_DataZoom.bottom + dataHig);
                    if (i > 0)
                    {
                        Color color = m_ThemeInfo.dataZoomLineColor;
                        ChartHelper.DrawLine(vh, lp, np, m_Coordinate.tickness, color);
                        Vector3 alp = new Vector3(lp.x, lp.y - m_Coordinate.tickness);
                        Vector3 anp = new Vector3(np.x, np.y - m_Coordinate.tickness);
                        Color areaColor = new Color(color.r, color.g, color.b, color.a * 0.75f);
                        Vector3 tnp = new Vector3(np.x, m_DataZoom.bottom + m_Coordinate.tickness);
                        Vector3 tlp = new Vector3(lp.x, m_DataZoom.bottom + m_Coordinate.tickness);
                        ChartHelper.DrawPolygon(vh, alp, anp, tnp, tlp, areaColor);
                    }
                    lp = np;
                }
            }
            switch (m_DataZoom.rangeMode)
            {
                case DataZoom.RangeMode.Percent:
                    var start = coordinateX + coordinateWid * m_DataZoom.start / 100;
                    var end = coordinateX + coordinateWid * m_DataZoom.end / 100;
                    p1 = new Vector2(start, m_DataZoom.bottom);
                    p2 = new Vector2(start, m_DataZoom.bottom + m_DataZoom.height);
                    p3 = new Vector2(end, m_DataZoom.bottom + m_DataZoom.height);
                    p4 = new Vector2(end, m_DataZoom.bottom);
                    ChartHelper.DrawPolygon(vh, p1, p2, p3, p4, m_ThemeInfo.dataZoomSelectedColor);
                    ChartHelper.DrawLine(vh, p1, p2, m_Coordinate.tickness, m_ThemeInfo.dataZoomSelectedColor);
                    ChartHelper.DrawLine(vh, p3, p4, m_Coordinate.tickness, m_ThemeInfo.dataZoomSelectedColor);
                    break;
            }
        }

        protected void DrawSplitLine(VertexHelper vh, bool isYAxis, Axis.SplitLineType type,
            Vector3 startPos, Vector3 endPos, Color color)
        {
            switch (type)
            {
                case Axis.SplitLineType.Dashed:
                case Axis.SplitLineType.Dotted:
                    var startX = startPos.x;
                    var startY = startPos.y;
                    var dashLen = type == Axis.SplitLineType.Dashed ? 6 : 2.5f;
                    var count = isYAxis ? (endPos.x - startPos.x) / (dashLen * 2) :
                        (endPos.y - startPos.y) / (dashLen * 2);
                    for (int i = 0; i < count; i++)
                    {
                        if (isYAxis)
                        {
                            var toX = startX + dashLen;
                            ChartHelper.DrawLine(vh, new Vector3(startX, startY), new Vector3(toX, startY),
                                m_Coordinate.tickness, color);
                            startX += dashLen * 2;
                        }
                        else
                        {
                            var toY = startY + dashLen;
                            ChartHelper.DrawLine(vh, new Vector3(startX, startY), new Vector3(startX, toY),
                                m_Coordinate.tickness, color);
                            startY += dashLen * 2;
                        }

                    }
                    break;
                case Axis.SplitLineType.Solid:
                    ChartHelper.DrawLine(vh, startPos, endPos, m_Coordinate.tickness, color);
                    break;
            }
        }

        protected void DrawXTooltipIndicator(VertexHelper vh)
        {
            if (!m_Tooltip.show || !m_Tooltip.IsSelected()) return;
            if (m_Tooltip.type == Tooltip.Type.None) return;

            for (int i = 0; i < m_XAxises.Count; i++)
            {
                var xAxis = m_XAxises[i];
                var yAxis = m_YAxises[i];
                if (!xAxis.show) continue;
                float splitWidth = xAxis.GetDataWidth(coordinateWid, m_DataZoom);
                switch (m_Tooltip.type)
                {
                    case Tooltip.Type.Corss:
                    case Tooltip.Type.Line:
                        float pX = coordinateX + m_Tooltip.xValues[i] * splitWidth
                            + (xAxis.boundaryGap ? splitWidth / 2 : 0);
                        if (xAxis.IsValue()) pX = m_Tooltip.pointerPos.x;
                        Vector2 sp = new Vector2(pX, coordinateY);
                        Vector2 ep = new Vector2(pX, coordinateY + coordinateHig);
                        DrawSplitLine(vh, false, Axis.SplitLineType.Solid, sp, ep, m_ThemeInfo.tooltipLineColor);
                        if (m_Tooltip.type == Tooltip.Type.Corss)
                        {
                            sp = new Vector2(coordinateX, m_Tooltip.pointerPos.y);
                            ep = new Vector2(coordinateX + coordinateWid, m_Tooltip.pointerPos.y);
                            DrawSplitLine(vh, true, Axis.SplitLineType.Dashed, sp, ep, m_ThemeInfo.tooltipLineColor);
                        }
                        break;
                    case Tooltip.Type.Shadow:
                        float tooltipSplitWid = splitWidth < 1 ? 1 : splitWidth;
                        pX = coordinateX + splitWidth * m_Tooltip.xValues[i] -
                            (xAxis.boundaryGap ? 0 : splitWidth / 2);
                        if (xAxis.IsValue()) pX = m_Tooltip.xValues[i];
                        float pY = coordinateY + coordinateHig;
                        Vector3 p1 = new Vector3(pX, coordinateY);
                        Vector3 p2 = new Vector3(pX, pY);
                        Vector3 p3 = new Vector3(pX + tooltipSplitWid, pY);
                        Vector3 p4 = new Vector3(pX + tooltipSplitWid, coordinateY);
                        ChartHelper.DrawPolygon(vh, p1, p2, p3, p4, m_ThemeInfo.tooltipFlagAreaColor);
                        break;
                }
            }
        }

        protected void DrawYTooltipIndicator(VertexHelper vh)
        {
            if (!m_Tooltip.show || !m_Tooltip.IsSelected()) return;
            if (m_Tooltip.type == Tooltip.Type.None) return;

            for (int i = 0; i < m_YAxises.Count; i++)
            {
                var yAxis = m_YAxises[i];
                if (!yAxis.show) continue;
                float splitWidth = yAxis.GetDataWidth(coordinateHig, m_DataZoom);
                switch (m_Tooltip.type)
                {
                    case Tooltip.Type.Corss:
                    case Tooltip.Type.Line:

                        float pY = coordinateY + m_Tooltip.yValues[i] * splitWidth + (yAxis.boundaryGap ? splitWidth / 2 : 0);
                        Vector2 sp = new Vector2(coordinateX, pY);
                        Vector2 ep = new Vector2(coordinateX + coordinateWid, pY);
                        DrawSplitLine(vh, false, Axis.SplitLineType.Solid, sp, ep, m_ThemeInfo.tooltipLineColor);
                        if (m_Tooltip.type == Tooltip.Type.Corss)
                        {
                            sp = new Vector2(coordinateX, m_Tooltip.pointerPos.y);
                            ep = new Vector2(coordinateX + coordinateWid, m_Tooltip.pointerPos.y);
                            DrawSplitLine(vh, true, Axis.SplitLineType.Dashed, sp, ep, m_ThemeInfo.tooltipLineColor);
                        }
                        break;
                    case Tooltip.Type.Shadow:
                        float tooltipSplitWid = splitWidth < 1 ? 1 : splitWidth;
                        float pX = coordinateX + coordinateWid;
                        pY = coordinateY + splitWidth * m_Tooltip.yValues[i] -
                            (yAxis.boundaryGap ? 0 : splitWidth / 2);
                        Vector3 p1 = new Vector3(coordinateX, pY);
                        Vector3 p2 = new Vector3(coordinateX, pY + tooltipSplitWid);
                        Vector3 p3 = new Vector3(pX, pY + tooltipSplitWid);
                        Vector3 p4 = new Vector3(pX, pY);
                        ChartHelper.DrawPolygon(vh, p1, p2, p3, p4, m_ThemeInfo.tooltipFlagAreaColor);
                        break;
                }
            }
        }

        private void CheckDataZoom()
        {
            if (raycastTarget != m_DataZoom.show)
            {
                raycastTarget = m_DataZoom.show;
            }
            if (!m_DataZoom.show) return;
            if (m_DataZoom.showDetail)
            {
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                    Input.mousePosition, null, out local))
                {
                    m_DataZoom.SetLabelActive(false);
                    return;
                }
                if (m_DataZoom.IsInSelectedZoom(local, coordinateX, coordinateWid)
                    || m_DataZoom.IsInStartZoom(local, coordinateX, coordinateWid)
                    || m_DataZoom.IsInEndZoom(local, coordinateX, coordinateWid))
                {
                    m_DataZoom.SetLabelActive(true);
                    RefreshDataZoomLabel();
                }
                else
                {
                    m_DataZoom.SetLabelActive(false);
                }
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 pos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                eventData.position, canvas.worldCamera, out pos))
            {
                return;
            }
            if (m_DataZoom.IsInStartZoom(pos, coordinateX, coordinateWid))
            {
                m_DataZoom.isDraging = true;
                m_DataZoomStartDrag = true;
            }
            else if (m_DataZoom.IsInEndZoom(pos, coordinateX, coordinateWid))
            {
                m_DataZoom.isDraging = true;
                m_DataZoomEndDrag = true;
            }
            else if (m_DataZoom.IsInSelectedZoom(pos, coordinateX, coordinateWid))
            {
                m_DataZoom.isDraging = true;
                m_DataZoomDrag = true;
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            float deltaX = eventData.delta.x;
            float deltaPercent = deltaX / coordinateWid * 100;
            if (m_DataZoomStartDrag)
            {
                m_DataZoom.start += deltaPercent;
                if (m_DataZoom.start < 0)
                {
                    m_DataZoom.start = 0;
                }
                else if (m_DataZoom.start > m_DataZoom.end)
                {
                    m_DataZoom.start = m_DataZoom.end;
                    m_DataZoomEndDrag = true;
                    m_DataZoomStartDrag = false;
                }
                RefreshDataZoomLabel();
                RefreshChart();
            }
            else if (m_DataZoomEndDrag)
            {
                m_DataZoom.end += deltaPercent;
                if (m_DataZoom.end > 100)
                {
                    m_DataZoom.end = 100;
                }
                else if (m_DataZoom.end < m_DataZoom.start)
                {
                    m_DataZoom.end = m_DataZoom.start;
                    m_DataZoomStartDrag = true;
                    m_DataZoomEndDrag = false;
                }
                RefreshDataZoomLabel();
                RefreshChart();
            }
            else if (m_DataZoomDrag)
            {
                if (deltaPercent > 0)
                {
                    if (m_DataZoom.end + deltaPercent > 100)
                    {
                        deltaPercent = 100 - m_DataZoom.end;
                    }
                }
                else
                {
                    if (m_DataZoom.start + deltaPercent < 0)
                    {
                        deltaPercent = -m_DataZoom.start;
                    }
                }
                m_DataZoom.start += deltaPercent;
                m_DataZoom.end += deltaPercent;
                RefreshDataZoomLabel();
                RefreshChart();
            }
        }

        private void RefreshDataZoomLabel()
        {
            var xAxis = m_XAxises[m_DataZoom.xAxisIndex];
            var startIndex = (int)((xAxis.data.Count - 1) * m_DataZoom.start / 100);
            var endIndex = (int)((xAxis.data.Count - 1) * m_DataZoom.end / 100);
            if (m_DataZoomLastStartIndex != startIndex || m_DataZoomLastEndIndex != endIndex)
            {
                m_DataZoomLastStartIndex = startIndex;
                m_DataZoomLastEndIndex = endIndex;
                if (xAxis.data.Count > 0)
                {
                    m_DataZoom.SetStartLabelText(xAxis.data[startIndex]);
                    m_DataZoom.SetEndLabelText(xAxis.data[endIndex]);
                }
                InitAxisX();
            }

            var start = coordinateX + coordinateWid * m_DataZoom.start / 100;
            var end = coordinateX + coordinateWid * m_DataZoom.end / 100;
            m_DataZoom.startLabel.transform.localPosition =
                new Vector3(start - 10, m_DataZoom.bottom + m_DataZoom.height / 2);
            m_DataZoom.endLabel.transform.localPosition =
                new Vector3(end + 10, m_DataZoom.bottom + m_DataZoom.height / 2);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (m_DataZoomDrag || m_DataZoomStartDrag || m_DataZoomEndDrag)
            {
                RefreshChart();
            }
            m_DataZoomDrag = false;
            m_DataZoomStartDrag = false;
            m_DataZoomEndDrag = false;
            m_DataZoom.isDraging = false;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            Vector2 localPos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                eventData.position, canvas.worldCamera, out localPos))
            {
                return;
            }
            if (m_DataZoom.IsInStartZoom(localPos, coordinateX, coordinateWid) ||
                m_DataZoom.IsInEndZoom(localPos, coordinateX, coordinateWid))
            {
                return;
            }
            if (m_DataZoom.IsInZoom(localPos, coordinateX, coordinateWid)
                && !m_DataZoom.IsInSelectedZoom(localPos, coordinateX, coordinateWid))
            {
                var pointerX = localPos.x;
                var selectWidth = coordinateWid * (m_DataZoom.end - m_DataZoom.start) / 100;
                var startX = pointerX - selectWidth / 2;
                var endX = pointerX + selectWidth / 2;
                if (startX < coordinateX)
                {
                    startX = coordinateX;
                    endX = coordinateX + selectWidth;
                }
                else if (endX > coordinateX + coordinateWid)
                {
                    endX = coordinateX + coordinateWid;
                    startX = coordinateX + coordinateWid - selectWidth;
                }
                m_DataZoom.start = (startX - coordinateX) / coordinateWid * 100;
                m_DataZoom.end = (endX - coordinateX) / coordinateWid * 100;
                RefreshDataZoomLabel();
                RefreshChart();
            }
        }

        public override void OnScroll(PointerEventData eventData)
        {
            if (!m_DataZoom.show || m_DataZoom.zoomLock) return;
            float deltaPercent = Mathf.Abs(eventData.scrollDelta.y *
                m_DataZoom.scrollSensitivity / coordinateWid * 100);
            if (eventData.scrollDelta.y > 0)
            {
                if (m_DataZoom.end <= m_DataZoom.start) return;
                m_DataZoom.end -= deltaPercent;
                m_DataZoom.start += deltaPercent;
                if (m_DataZoom.end <= m_DataZoom.start)
                {
                    m_DataZoom.end = m_DataZoom.start;
                }
            }
            else
            {
                m_DataZoom.end += deltaPercent;
                m_DataZoom.start -= deltaPercent;
                if (m_DataZoom.end > 100) m_DataZoom.end = 100;
                if (m_DataZoom.start < 0) m_DataZoom.start = 0;
            }
            RefreshDataZoomLabel();
            RefreshChart();
        }
    }
}

