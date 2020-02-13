using SharpDX.Direct2D1;
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
        public int N;
        public float[] datas;
        List<float> scaleDataList = new List<float>();

        public ChartM(ScMgr scmgr = null)
            : base(scmgr)
        {
            SizeChanged += ScPanel_SizeChanged;
            D2DPaint += ScPanel_D2DPaint;
        }


        private void ScPanel_SizeChanged(object sender, SizeF oldSize)
        {
            ScaleDatas();
        }

        void ScaleDatas()
        {
            scaleDataList.Clear();
            float max = 0;

            for(int i=0; i<datas.Length; i++)
            {
                if (Math.Abs(datas[i]) > max)
                    max = Math.Abs(datas[i]);
            }

            float scale = (Height / 2) / max;

            for(int i = 0; i < datas.Length; i++)
            {
                scaleDataList.Add(datas[i] * scale);
            }

        }

        private void ScPanel_D2DPaint2(D2DGraphics g)
        {
            g.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            SolidColorBrush brush = new SolidColorBrush(g.RenderTarget, new RawColor4(0,0,0,1));
            float baselineY = Height / 2;
            float step = Width / (scaleDataList.Count - 1);

            RawVector2 pt1 = new RawVector2(0, -scaleDataList[0] + baselineY);
            RawVector2 pt2 = new RawVector2();

            for (int i=0; i< scaleDataList.Count; i++)
            {
                pt2.X = i * step;
                pt2.Y = -scaleDataList[i] + baselineY;
                g.RenderTarget.DrawLine(pt1, pt2, brush, 0.3f);

                pt1 = pt2;
            }

        }

      
        private void ScPanel_D2DPaint(D2DGraphics g)
        {
            g.RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            SolidColorBrush brush = new SolidColorBrush(g.RenderTarget, new RawColor4(1, 0, 0, 1));

            float baseSeq = (float)44100 / N;


            float baselineY = Height / 2;



            float step = Width / 20000;


            RawVector2 pt1 = new RawVector2(0, -scaleDataList[0] + baselineY);
            RawVector2 pt2 = new RawVector2();

            for (int i = 1; i < scaleDataList.Count; i++)
            {
                pt2.X = baseSeq * i * step;
                pt2.Y = -scaleDataList[i] + baselineY;
                g.RenderTarget.DrawLine(pt1, pt2, brush, 0.5f);
                pt1 = pt2;
            }

            //
            StrokeStyleProperties ssp = new StrokeStyleProperties();
            ssp.DashStyle = DashStyle.DashDot;
            StrokeStyle strokeStyle = new StrokeStyle(D2DGraphics.d2dFactory, ssp);
            SolidColorBrush brush2 = new SolidColorBrush(g.RenderTarget, new RawColor4(0, 0, 0, 1));
            g.RenderTarget.DrawLine(new RawVector2(0, Height/2), new RawVector2(Width, Height/2), brush2, 0.5f, strokeStyle);

            //
            float widthStep = Width / 10.0f;
            float seqStep = 20000 / 10.0f;
            RawRectangleF rect;

            for (int i=0; i<10; i++)
            {
                SolidColorBrush brushx = new SolidColorBrush(g.RenderTarget, new RawColor4(0, 0, 0, 1));
                TextFormat textFormat = new TextFormat(D2DGraphics.dwriteFactory, "微软雅黑", 10)
                { TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Center };

                textFormat.WordWrapping = WordWrapping.Wrap;

                float x = (widthStep * i - 100 + widthStep * i + 100) / 2f;


                g.RenderTarget.DrawLine(new RawVector2(x, Height / 2), new RawVector2(x, Height / 2 + 3), brush2, 1f);

                rect = new RawRectangleF(widthStep * i - 100, Height/2, widthStep * i + 100, Height/2 + 15);
                string str = (i * seqStep).ToString();
                g.RenderTarget.DrawText(str, textFormat, rect, brush2, DrawTextOptions.Clip);
            }


            //RawRectangleF rect = new RawRectangleF(0, 1, Width - 1, Height -1);
            //RawColor4 rawColor = GDIDataD2DUtils.TransToRawColor4(Color.FromArgb(255, 255, 0, 0));
            //SolidColorBrush brush2 = new SolidColorBrush(g.RenderTarget, rawColor);
            //g.RenderTarget.DrawRectangle(rect, brush2, 5);

            //rect = new RawRectangleF(0, 0, Width - 1, 10 - 1);
            //rawColor = GDIDataD2DUtils.TransToRawColor4(Color.FromArgb(255, 122, 151, 207));
            //brush = new SolidColorBrush(g.RenderTarget, rawColor);
            //g.RenderTarget.FillRectangle(rect, brush);


            //rect = new RawRectangleF(1, 1, Width - 1, Height - 1);
            //rawColor = GDIDataD2DUtils.TransToRawColor4(Color.FromArgb(255, 214, 215, 220));
            //brush = new SolidColorBrush(g.RenderTarget, rawColor);
            //g.RenderTarget.DrawRectangle(rect, brush);
        }
    }
}