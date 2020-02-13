using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kermalis.SoundFont2;
using Sc;

namespace SoundFontTest
{
    public class ChartApp
    {
        ScMgr scMgr;
        ScLayer root;
        byte[] pcm_buffer;
        PCMProcesser pcmProcesser;
        short[] pcm;
        public ChartApp(ScMgr scMgr)
        {
            SF2 sf2 = new SF2("g:\\test.sf2");
            List<short> samples = sf2.SoundChunk.SMPLSubChunk.samples;
            pcmProcesser = new PCMProcesser();
            short[] newSamples = pcmProcesser.PitchPcmNote(samples.ToArray(),-24, true, 10);

            pcm_buffer = new byte[newSamples.Length * 2];
            int idx = 0;
            for (int i = 0; i < newSamples.Length; i++)
            {
                pcm_buffer[idx++] = (byte)(newSamples[i] & 0xff);
                pcm_buffer[idx++] = (byte)((newSamples[i] >> 8) & 0xff);
            }


            List<short> shortBufs = new List<short>();
            short val;
            for (int i = 0; i < pcm_buffer.Length; i+=2)
            {
                val = (short)((pcm_buffer[i + 1] << 8) | pcm_buffer[i]);
                shortBufs.Add(val);
            }

            List<float> floatBufs = new List<float>();
            for (int i = 0; i < pcm_buffer.Length; i += 2)
            {
                val = (short)((pcm_buffer[i + 1] << 8) | pcm_buffer[i]);
                floatBufs.Add(val);
            }

            pcm = shortBufs.ToArray();
            float[] seqDatas = pcmProcesser.CreatePcmFreqNormAmpSpectrum(pcm, 44100);

            List<float> samplesFloat = new List<float>();
            for (int i = 0; i < shortBufs.Count; i++)
            {
                samplesFloat.Add(shortBufs[i]);
            }


            this.scMgr = scMgr;
            root = scMgr.GetRootLayer();

            ChartM chart = new ChartM(scMgr);
            chart.CreateAxisXSeq = CreateAxisXSeq;
            chart.Datas = samplesFloat.ToArray();
           // chart.DataLineColor = Color.Blue;
          //  chart.XAxisColor = Color.White;
            chart.StartDataIdx = 0;
            chart.EndDataIdx = 0;
            chart.xAxisSeqCount = 10;
            chart.Dock = ScDockStyle.Fill;
           // chart.BackgroundColor = Color.Black;
            root.Add(chart);
        }

        float CreateAxisXSeq()
        {
            return pcmProcesser.GetPcmBaseFreq(44100, pcm.Length);

           // return 1;
        }


    }
}
