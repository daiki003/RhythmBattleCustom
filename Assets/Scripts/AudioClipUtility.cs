using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

public static class AudioClipUtility
{
    public static float EstimateBPM(AudioClip clip)
    {
        int channels = clip.channels;
        int totalSamples = clip.samples;

        // ステレオ→モノラル
        var mono = MonoConversion(clip, channels, totalSamples);

        int windowSize = 1024;
        int hopSize = 512;

        // スペクトルフラックス生成（Novelty Curve）
        List<float> noveltyCurve = new List<float>();
        float[] prevMag = null;

        for (int i = 0; i < mono.Length - windowSize; i += hopSize)
        {
            Complex[] buffer = new Complex[windowSize];
            for (int j = 0; j < windowSize; j++)
                buffer[j] = new Complex(mono[i + j], 0);

            Fourier.Forward(buffer, FourierOptions.Matlab);
            float[] mag = buffer.Select(c => (float)c.Magnitude).ToArray();

            if (prevMag != null)
            {
                float flux = 0f;
                for (int k = 0; k < mag.Length; k++)
                {
                    float diff = mag[k] - prevMag[k];
                    if (diff > 0) flux += diff;
                }
                noveltyCurve.Add(flux);
            }

            prevMag = mag;
        }

        // 平滑化（移動平均なども可）
        float maxFlux = noveltyCurve.Max();
        for (int i = 0; i < noveltyCurve.Count; i++)
            noveltyCurve[i] /= maxFlux;

        // テンポスペクトラム生成（周期性検出：FFT）
        int n = noveltyCurve.Count;
        Complex[] noveltyFFT = new Complex[n];
        for (int i = 0; i < n; i++)
            noveltyFFT[i] = new Complex(noveltyCurve[i], 0);

        Fourier.Forward(noveltyFFT, FourierOptions.Matlab);

        // パワースペクトル（振幅の2乗）
        float[] powerSpectrum = new float[n / 2];
        for (int i = 0; i < powerSpectrum.Length; i++)
            powerSpectrum[i] = (float)(noveltyFFT[i].Magnitude * noveltyFFT[i].Magnitude);

        int sampleRate = clip.frequency;

        // 周期 → BPM に変換
        Dictionary<float, float> bpmStrength = new();
        for (int i = 1; i < powerSpectrum.Length; i++)
        {
            float freqHz = i * sampleRate / (float)(hopSize * n); // 周波数[Hz]
            if (freqHz < 0.5f || freqHz > 4.0f) continue; // BPM 30〜240 に相当

            float bpm = freqHz * 60f; // 周波数 → BPM
            if (bpm < 60 || bpm > 200) continue;

            if (!bpmStrength.ContainsKey(bpm))
                bpmStrength[bpm] = 0f;
            bpmStrength[bpm] += powerSpectrum[i];
        }

        if (bpmStrength.Count == 0)
            return 0;

        // 最強ピーク（最大振幅のBPM）を選択
        float bestBPM = bpmStrength.OrderByDescending(kv => kv.Value).First().Key;
        return Mathf.Round(bestBPM * 100f) / 100f;
    }

    public static float GetStartSoundTime(AudioClip clip, float threshold = 0.01f, int windowMs = 20)
    {
        int channels = clip.channels;
        int totalSamples = clip.samples;

        // モノラル変換
        float[] mono = MonoConversion(clip, channels, totalSamples);

        int sampleRate = clip.frequency;

        // ウィンドウサイズ（ms → サンプル数）
        int windowSize = Mathf.CeilToInt(sampleRate * windowMs / 1000f);
        windowSize = Mathf.Clamp(windowSize, 1, totalSamples);

        // ウィンドウごとのRMS（パワー）を計算
        for (int i = 0; i < mono.Length - windowSize; i += windowSize / 4) // 75%重複で精度UP
        {
            float sumSq = 0f;
            for (int j = 0; j < windowSize; j++)
            {
                float s = mono[i + j];
                sumSq += s * s;
            }
            float rms = Mathf.Sqrt(sumSq / windowSize);

            if (rms > threshold)
            {
                return Mathf.Round(i / (float)sampleRate * 100f) / 100f;
            }
        }
        return 0f; // 完全な無音
    }

    public static List<float> DetectNoteTimings(AudioClip clip, float strengthThreshold = 0.03f, float minSpacing = 0.3f)
    {
        int channels = clip.channels;
        int samples = clip.samples;

        // モノラル変換
        float[] mono = MonoConversion(clip, channels, samples);

        // Novelty Curve生成（スペクトルフラックス）
        int windowSize = 1024;
        int hopSize = 512;

        List<float> novelty = new List<float>();
        List<float> times = new List<float>();
        float[] prevMag = null;

        int sampleRate = clip.frequency;
        for (int i = 0; i < mono.Length - windowSize; i += hopSize)
        {
            Complex[] buf = new Complex[windowSize];
            for (int j = 0; j < windowSize; j++)
                buf[j] = new Complex(mono[i + j], 0);

            Fourier.Forward(buf, FourierOptions.Matlab);
            float[] mag = buf.Select(c => (float)c.Magnitude).ToArray();

            if (prevMag != null)
            {
                float flux = 0f;
                for (int k = 0; k < mag.Length; k++)
                {
                    float diff = mag[k] - prevMag[k];
                    if (diff > 0) flux += diff;
                }

                novelty.Add(flux);
                times.Add((float)i / sampleRate);
            }

            prevMag = mag;
        }

        // 正規化（0〜1）
        float max = novelty.Max();
        if (max > 0f)
            for (int i = 0; i < novelty.Count; i++)
                novelty[i] /= max;

        // ローカルピークかつ一定強度・間隔を満たすものだけ残す
        List<float> noteTimings = new List<float>();
        float lastTime = -999f;

        for (int i = 1; i < novelty.Count - 1; i++)
        {
            if (novelty[i] > strengthThreshold &&
                novelty[i] > novelty[i - 1] &&
                novelty[i] > novelty[i + 1])
            {
                float time = times[i];
                if (time - lastTime >= minSpacing)
                {
                    noteTimings.Add((float)Math.Round(time, 3));
                    lastTime = time;
                }
            }
        }

        return noteTimings;
    }

    private static float[] MonoConversion(AudioClip clip, int channels, int totalSamples)
    {
        float[] raw = new float[totalSamples * channels];
        clip.GetData(raw, 0);

        // モノラル変換
        float[] mono = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++)
            {
                sum += raw[i * channels + c];
            }
            mono[i] = sum / channels;
        }
        return mono;
    }
}
