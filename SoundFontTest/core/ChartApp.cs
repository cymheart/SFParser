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
        public ChartApp(ScMgr scMgr)
        {
            SF2 sf2 = new SF2("g:\\test.sf2");
            List<short> samples = sf2.SoundChunk.SMPLSubChunk.samples;
            PCMProcesser pcmProcesser = new PCMProcesser();
            short[] newSamples = pcmProcesser.PitchPcmNote(samples.ToArray(),24);

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

            float[] seqDatas = pcmProcesser.CreatePcmFreqNormAmpSpectrum(shortBufs.ToArray(), 44100);

            List<float> samplesFloat = new List<float>();
            for (int i = 0; i < shortBufs.Count; i++)
            {
                samplesFloat.Add(shortBufs[i]);
            }


            this.scMgr = scMgr;
            root = scMgr.GetRootLayer();

            ChartM chart = new ChartM(scMgr);
            chart.N = pcm_buffer.Length/2;
            chart.datas = seqDatas;
            chart.Dock = ScDockStyle.Fill;
            chart.BackgroundColor = Color.White;
            root.Add(chart);
        }


    }
}
