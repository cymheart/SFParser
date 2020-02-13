using FFTWSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SoundFontTest
{
  
    public class PCMProcesser
    {
        /// <summary>
        /// 半音之间的频率倍率2^(1/12) = 1.059463f
        /// </summary>
        static float semitFreqMul = 1.059463f;

        int smoothRadius = -1;
        private float[] kernel;
        private float[] normalKernel;
        private float[] kernelSum;

        public float GetPcmBaseFreq(float pcmSampleFreq, float pcmSampleCount)
        {       
           return pcmSampleFreq / pcmSampleCount;
        }

        public int GetPcmFreqIdx(float getFreq, float pcmSampleFreq, float pcmSampleCount)
        {
            float baseFreq = pcmSampleFreq / pcmSampleCount;
            int idx = (int)Math.Round(getFreq / baseFreq);
            return Math.Min(idx, (int)(pcmSampleCount - 1));
        }

        public float GetPcmTime(short[] pcm, float pcmSampleFreq)
        {
            return pcm.Length / pcmSampleFreq;
        }

        public float GetPcmTimeForRightOffset(short[] pcm, float pcmSampleFreq, uint semitCount)
        {
            float pcmTime = pcm.Length / pcmSampleFreq;
            float mul = (float)Math.Pow(semitFreqMul, semitCount);
            float newPcmTime = pcmTime / mul;
            return newPcmTime;
        }

        public float GetPcmTimeForLeftOffset(short[] pcm, float pcmSampleFreq, uint semitCount)
        {
            float pcmTime = pcm.Length / pcmSampleFreq;
            float mul = (float)Math.Pow(semitFreqMul, semitCount);
            float newPcmTime = pcmTime * mul;
            return newPcmTime;
        }

        public short[] PitchPcmNote(short[] pcm, int semitCount, bool isSmooth = true, int smoothRadius = 10)
        {
            if (semitCount > 0)
                return RightOffsetPcmNote(pcm, (uint)semitCount);
            else
                return LeftOffsetPcmNote(pcm, (uint)-semitCount, isSmooth, smoothRadius);
        }


        public short[] RightOffsetPcmNote(short[] pcm, uint semitCount)
        {
            float mul = (float)Math.Pow(semitFreqMul, semitCount);
            float oldCount = pcm.Length;
            int count = (int)(oldCount / mul);
            short[] newPcmBuf = new short[count];

            int idx;
            for (int i = 0; i < count; i++)
            {
                idx = (int)Math.Round(i * mul);
                newPcmBuf[i] = pcm[idx];
            }

            return newPcmBuf;
        }


        public short[] LeftOffsetPcmNote(short[] pcm, uint semitCount, bool isSmooth = true, int smoothRadius = 10)
        {
            float mul = (float)Math.Pow(semitFreqMul, semitCount);
            int count = (int)(pcm.Length * mul);
            short[] newPcmBuf = new short[count];

            short prevVal = 0, lastVal;
            int lastIdx;
            int prevIdx = 0;
            float stepVal;
            short result;

            newPcmBuf[0] = pcm[0];

            for (int i = 1; i < pcm.Length; i++)
            {
                lastIdx = (int)Math.Round(i * mul);
                lastVal = pcm[i];
                stepVal = (lastVal - prevVal) / (float)(lastIdx - prevIdx);

                for (int j = prevIdx + 1; j < lastIdx; j++)
                {
                    result = (short)(prevVal + (int)(stepVal * (j - prevIdx)));
                    newPcmBuf[j] = result;
                }

                newPcmBuf[lastIdx] = pcm[i];

                prevVal = lastVal;
                prevIdx = lastIdx;
            }

            if(isSmooth)
                newPcmBuf = SmoothPcm(newPcmBuf, smoothRadius);

            return newPcmBuf;
        }

        public short[] SmoothPcm(short[] pcm, int radius = 10)
        {
            int count = pcm.Length;
            short[] resultPcm = new short[pcm.Length];

            if (radius != smoothRadius)
            {
                PreCalculatePCMSmoothKernel(radius);
                smoothRadius = radius;
            }

            float sum;
            float calValue;
            int s;
            for (int i = 0; i < radius; i++)
            {
                sum = kernelSum[i];
                s = radius - i;
                calValue = 0;

                for (int j = 0; j <= i + radius; j++)
                {
                    calValue += kernel[s++] * pcm[j];
                }

                calValue *= sum;
                resultPcm[i] = (short)calValue;
            }


            //
            for (int i = radius; i < count - radius; i++)
            {
                s = 0;
                calValue = 0;
                for (int j = i - radius; j <= i + radius; j++)
                {
                    calValue += normalKernel[s++] * pcm[j];
                }
                resultPcm[i] = (short)calValue;
            }

            //
            for (int i = count - radius; i < count; i++)
            {
                sum = kernelSum[count - i - 1];
                s = 0;
                calValue = 0;

                for (int j = i - radius; j <= count - 1; j++)
                {
                    calValue += kernel[s++] * pcm[j];
                }

                calValue *= sum;
                resultPcm[i] = (short)calValue;
            }

            return resultPcm;
        }


        /// <summary>
        /// 生成PCM的频谱
        /// </summary>
        /// <param name="pcm">音频流</param>
        /// <returns></returns>
        public float[] CreatePcmFreqSpectrum(short[] pcm)
        {
            IntPtr pin, pout;
            pin = fftwf.malloc(pcm.Length * 8);
            pout = fftwf.malloc(pcm.Length * 8);

            float[] fin = new float[pcm.Length * 2];

            for (int i = 0; i < pcm.Length; i++)
            {
                fin[i * 2] = pcm[i];
                fin[i * 2 + 1] = 0;
            }

            Marshal.Copy(fin, 0, pin, pcm.Length * 2);

            IntPtr fplan = fftwf.dft_1d(pcm.Length, pin, pout, fftw_direction.Forward, fftw_flags.Estimate);
            fftwf.execute(fplan);

            float[] fout = new float[pcm.Length * 2];
            Marshal.Copy(pout, fout, 0, pcm.Length * 2);

            fftwf.free(pin);
            fftwf.free(pout);
            fftwf.destroy_plan(fplan);

            return fout;
        }

        /// <summary>
        /// 生成PCM的频率归一幅度谱
        /// </summary>
        /// <param name="pcm">音频流</param>
        /// <param name="pcmSampleFreq">音频的采样频率</param>
        /// <returns></returns>
        public float[] CreatePcmFreqNormAmpSpectrum(short[] pcm, float pcmSampleFreq)
        {
            float[] pcmFreqSpectrum = CreatePcmFreqSpectrum(pcm);
            return CreatePcmFreqNormAmpSpectrum(pcmFreqSpectrum, pcmSampleFreq);
        }

        /// <summary>
        /// 生成PCM的频率归一幅度谱
        /// </summary>
        /// <param name="pcmFreqSpectrum">音频流频谱</param>
        /// <param name="pcmSampleFreq">音频的采样频率</param>
        /// <returns></returns>
        public float[] CreatePcmFreqNormAmpSpectrum(float[] pcmFreqSpectrum, float pcmSampleFreq)
        {       
            float n;
            float max = 0;
            float baseFreq = pcmSampleFreq / (pcmFreqSpectrum.Length / 2);

            List<float> result = new List<float>();

            for(int i=0; i < pcmFreqSpectrum.Length; i+=2)
            {
                if ((baseFreq * i)/2 > 20000)
                    break;

                n = pcmFreqSpectrum[i] * pcmFreqSpectrum[i] + pcmFreqSpectrum[i + 1] * pcmFreqSpectrum[i + 1];
                n = (float)Math.Sqrt(n);
                result.Add(n);

                if (n > max)
                    max = n;
            }

            for (int i = 0; i < result.Count; i++)
            {
                result[i] /= max;
            }

            return result.ToArray();

        }

           
        //预计算radius长度步距为1的x^2分布的所有值    
        //此处用x^2分布近似正态分布来计算像素的权重比值
        private void PreCalculatePCMSmoothKernel(int radius)
        {
            int sz = radius * 2 + 1;
            kernel = new float[sz];
            normalKernel = new float[sz];
            kernelSum = new float[radius + 1];
            float kernelTotalSum = 0;

            for (int i = 1; i <= radius; i++)
            {
                int szi = radius - i;
                int szj = radius + i;
                kernel[szj] = kernel[szi] = (szi + 1) * (szi + 1);
                kernelTotalSum += kernel[szj] + kernel[szi];
            }

            kernel[radius] = (radius + 1) * (radius + 1);
            kernelTotalSum += kernel[radius];


            float kernelHalfSum = (kernelTotalSum - kernel[radius]) / 2;
            kernelSum[radius] = kernelTotalSum;
            kernelSum[0] = kernel[radius] + kernelHalfSum;

            for (int i = 1; i < radius; i++)
            {
                kernelSum[i] = kernelSum[i - 1] + kernel[radius - i];
            }

            for (int i = 0; i < kernelSum.Length; i++)
            {
                kernelSum[i] = 1 / kernelSum[i];
            }

            for(int i=0; i<kernel.Length; i++)
            {
                normalKernel[i] = kernel[i] * kernelSum[radius];
            }
        }
    }
}
