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
        int sampleRate = clip.frequency;
        int channels = clip.channels;
        int totalSamples = clip.samples;


        // ステレオ→モノラル
        float[] rawData = new float[totalSamples * channels];
        clip.GetData(rawData, 0);

        float[] mono = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++)
                sum += rawData[i * channels + c];
            mono[i] = sum / channels;
        }

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
        int sampleRate = clip.frequency;
        int channels = clip.channels;
        int totalSamples = clip.samples;

        // データ取得
        float[] raw = new float[totalSamples * channels];
        clip.GetData(raw, 0);

        // モノラル変換
        float[] mono = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++)
                sum += raw[i * channels + c];
            mono[i] = sum / channels;
        }

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
}
