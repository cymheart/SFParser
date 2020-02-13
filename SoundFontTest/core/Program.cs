using FFTWSharp;
using Kermalis.SoundFont2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static SDL2.SDL;

namespace SoundFontTest
{
    class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            SetProcessDPIAware();  //重要
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
               Application.Run(new Form());

         //Test();
        }



        [DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();


        static byte[] pcm_buffer;
        static int audio_len;
        static int audio_idx;
        static byte[] audio_pos;



        static void Test()
        {
          
            SF2 sf2 = new SF2("g:\\test.sf2");

            List<short> samples = sf2.SoundChunk.SMPLSubChunk.samples;

            PCMProcesser pcmProcesser = new PCMProcesser();
            short[] newSamples = pcmProcesser.PitchPcmNote(samples.ToArray(), 24);

           // pitch.Seq(samples.ToArray());


            pcm_buffer = new byte[newSamples.Length * 2];
            int idx = 0;
            for(int i=0; i< newSamples.Length; i++)
            {
                pcm_buffer[idx++] = (byte)(newSamples[i] & 0xff);
                pcm_buffer[idx++] = (byte)((newSamples[i]>>8) & 0xff);
            }

          
            float newTime = pcmProcesser.GetPcmTime(newSamples, 44100);


            SDL_AudioSpec OutputAudioSpec = new SDL_AudioSpec();
            OutputAudioSpec.freq =  44100;
            OutputAudioSpec.format = AUDIO_S16;
            OutputAudioSpec.channels = 1;
            OutputAudioSpec.samples = 1024;
            OutputAudioSpec.callback = SDL_AudioCallback;


            if (SDL_AudioInit(null) < 0)
            {       
                return;
            }

            IntPtr n = IntPtr.Zero;
            if (SDL_OpenAudio(ref OutputAudioSpec, n) < 0)
            {
                return;
            }


            audio_len = pcm_buffer.Length; //长度为读出数据长度，在read_audio_data中做减法
            audio_pos = pcm_buffer;
            audio_idx = 0;

            SDL_PauseAudio(0);
            SDL_Delay((uint)(Math.Round(newTime)*1000));

        }

    
        static void SDL_AudioCallback(IntPtr userdata, IntPtr stream, int len)
        {
            if (audio_len == 0)
                return;

            byte[] tmps = new byte[len];
            Array.Clear(tmps, 0, len);
            Marshal.Copy(tmps, 0, stream, len);


            len = (len > audio_len ? audio_len : len);

          


            //stream = pcm_buffer;

            //	SDL_memcpy(stream, pcm_buffer, 100);

             byte[] dst = new byte[len];
             SDL_MixAudio(dst, audio_pos, (uint)len, SDL_MIX_MAXVOLUME/2);

            Marshal.Copy(dst, 0, stream, len);

            audio_len -= len;
            audio_pos = new byte[audio_len];
            audio_idx += len;
            Array.Copy(pcm_buffer, audio_idx, audio_pos, 0, audio_len);

        }

    }
}
