﻿using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sc;
using SharpDX.DirectWrite;

namespace SoundFontTest
{
    public class ChartM : ScLayer
    {

        public delegate float CreateAxisXSeqAction();
        public CreateAxisXSeqAction CreateAxisXSeq;

        public int StartDataIdx = 0;
        public int EndDataIdx = 0;
        public int xAxisSeqCount = 10;

        public Color DataLineColor = Color.Red;
        public Color XAxisColor = Color.Black;

        float maxAbsDataValue = 0;
        float scale;
        TextFormat textFormat;

        float[] datas;
        public float[] Datas
        {
            get { return datas; }
            set
            {
                datas = value;

                for (int i = 0; i < datas.Length; i++)
                {
                    if (Math.Abs(datas[i]) > maxAbsDataValue)
                        maxAbsDataValue = Math.Abs(datas[i]);
                }

            }
        }
        
        public ChartM(ScMgr scmgr = null)
            : base(scmgr)
        {
            textFormat = new TextFormat(D2DGraphics.dwriteFactory, "微软雅黑", 10)
            { TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Center, WordWrapping = WordWrapping.Wrap };

            SizeChanged += ScPanel_SizeChanged;
            D2DPaint += ScPanel_D2DPaint;
        }


        private void ScPanel_SizeChanged(object sender, SizeF oldSize)
        {
            scale = (Height / 2) / maxAbsDataValue;
        }

        int GetDrawDataCount()
        {
            int startIdx = StartDataIdx;
            int endIdx = EndDataIdx;
            if (endIdx <= 0)
                endIdx = datas.Length - 1;

            return endIdx - startIdx + 1;
        }


        int GetCombineIdx(int startIdx, int endIdx)
        {
            float startVal = datas[startIdx] * scale;
            int lastIdx = startIdx + 1;
            float d;

            int dataCount = GetDrawDataCount();

            for (int i = startIdx + 1; i <= endIdx; i++)
            {        
                d = Math.Abs(datas[i] * scale - startVal);


                if ((dataCount <= 20000 && d > 0.2f) ||
                    (dataCount > 20000 && dataCount <= 100000 && d > 0.2f) ||
                    (dataCount > 100000 && d > 0.2f))
                {
                    return lastIdx;
                }

                lastIdx = i;
            }

            return endIdx;
        }

        private void ScPanel_D2DPaint(D2DGraphics g)
        {
            DrawDatas(g);
            DrawAxisX(g);
        }

        void DrawDatas(D2DGraphics g)
        {
            g.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            SolidColorBrush brush = new SolidColorBrush(g.RenderTarget, GDIDataD2DUtils.TransToRawColor4(DataLineColor));

            float baselineY = Height / 2;
            int startIdx = StartDataIdx;
            int endIdx = EndDataIdx;
            if (endIdx <= 0)
                endIdx = datas.Length - 1;

            float step = Width / (endIdx - startIdx);

            int prevIdx = startIdx;
            int lastIdx = startIdx;
            RawVector2 pt1 = new RawVector2(0, -datas[startIdx] * scale + baselineY);
            RawVector2 pt2 = new RawVector2();


            while (lastIdx < endIdx)
            {
                lastIdx = GetCombineIdx(prevIdx, endIdx);
                pt2.X = (lastIdx - startIdx) * step;
                pt2.Y = -datas[lastIdx] * scale + baselineY;
                g.RenderTarget.DrawLine(pt1, pt2, brush, 0.5f);
                pt1 = pt2;
                prevIdx = lastIdx;
            }
        }

        void DrawAxisX(D2DGraphics g)
        {
            g.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            int startIdx = StartDataIdx;
            int endIdx = EndDataIdx;
            if (endIdx <= 0)
                endIdx = datas.Length - 1;

            StrokeStyleProperties ssp = new StrokeStyleProperties();
            ssp.DashStyle = DashStyle.DashDot;
            StrokeStyle strokeStyle = new StrokeStyle(D2DGraphics.d2dFactory, ssp);
            SolidColorBrush brush2 = new SolidColorBrush(g.RenderTarget, GDIDataD2DUtils.TransToRawColor4(XAxisColor));
            g.RenderTarget.DrawLine(new RawVector2(0, Height / 2), new RawVector2(Width, Height / 2), brush2, 0.5f, strokeStyle);

            //
            float widthStep = Width / xAxisSeqCount;

            float numSeq = CreateAxisXSeq(); 
            float startNum = startIdx * numSeq;
            float numWidth = (endIdx - startIdx) * numSeq;
            float numStep = numWidth / xAxisSeqCount;
            
            RawRectangleF rect;

            for (int i = 0; i < xAxisSeqCount; i++)
            {
                float x = (widthStep * i - 100 + widthStep * i + 100) / 2f;
                g.RenderTarget.DrawLine(new RawVector2(x, Height / 2), new RawVector2(x, Height / 2 + 3), brush2, 1f);

                //
                rect = new RawRectangleF(widthStep * i - 100, Height / 2, widthStep * i + 100, Height / 2 + 15);
                string str = (startNum + i * numStep).ToString("#.##");
                g.RenderTarget.DrawText(str, textFormat, rect, brush2, DrawTextOptions.Clip);
            }
        }       
    }
}