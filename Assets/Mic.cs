using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NSpeex;
using System;

[RequireComponent(typeof(AudioSource))]
public class Mic : MonoBehaviour {
    
    const bool usingSpeex = true;

    const int loopTIme = 1;
    const int samplingRate = 44100;
    const int encoderBitRate = 8192;
    const int targetSamplingRate = 8000;
    const int targetSamplingSize = 320;
    // const int targetSamplingRate = 16000;
    // const int targetSamplingSize = 640;

    MainDevice mainDevice;
    AudioSource recordAudio = null;
    AudioSource playAudio = null;
    ArrayList encodeBufList;
    int lastSamplePos = 0;
    int totalSampleSize;

    // NSpeex
    SpeexEncoder speexEncoder;
    SpeexDecoder speexDecoder;
    int recordSampleSize = 0;
    float[] sampleBuffer = null;
    int sampleIndex = 0;

    // Use this for initialization
    void Start () {
        mainDevice = GameObject.FindGameObjectWithTag("MainDevice").GetComponent<MainDevice>();
        recordAudio = gameObject.AddComponent<AudioSource>();
        playAudio = gameObject.AddComponent<AudioSource>();
        encodeBufList = null;
        lastSamplePos = 0;
    }

    void Update () {
        if (recordAudio && Microphone.IsRecording(null)) {
            string log = "";
            int currentPosition = Microphone.GetPosition(null);
            log += "Mic Pos : " + currentPosition;
            mainDevice.log(log);

            // This means we wrapped around
            if (currentPosition < lastSamplePos) {
                while (sampleIndex < samplingRate) {
                    ReadSample();
                }

                sampleIndex = 0;
            }

            // Read non-wrapped samples
            lastSamplePos = currentPosition;

            while (sampleIndex + recordSampleSize <= currentPosition) {
                ReadSample();
            }
        }
    }

    public void Resample (float[] src, float[] dst) {
        if (src.Length == dst.Length) {
            Array.Copy(src, 0, dst, 0, src.Length);
        } else {
            //TODO: Low-pass filter 
            float rec = 1.0f / (float)dst.Length;

            for (int i = 0; i < dst.Length; ++i) {
                float interp = rec * (float)i * (float)src.Length;
                dst[i] = src[(int)interp];
            }
        }
    }

    void ReadSample () {
        // Extract data
        recordAudio.clip.GetData(sampleBuffer, sampleIndex);

        // Grab a new sample buffer
        float[] targetSampleBuffer = new float[targetSamplingSize];

        // Resample our real sample into the buffer
        Resample(sampleBuffer, targetSampleBuffer);

        // Forward index
        sampleIndex += recordSampleSize;

        short[] data = ToShortArray(targetSampleBuffer);
        byte[] buf = new byte[recordSampleSize * 4];
        int len = speexEncoder.Encode(data, 0, data.Length, buf, 0, buf.Length);
        if (len != 0) {
            encodeBufList.AddRange(buf.Take(len).ToArray());
        }

        totalSampleSize += (recordSampleSize * 4);
    }

    public void RecordStart() {
        if (playAudio.isPlaying) {
            this.mainDevice.log("---RecordStop---");
            playAudio.Stop();
        }

        encodeBufList = new ArrayList();
        lastSamplePos = 0;
        sampleIndex = 0;
        totalSampleSize = 0;

        ShowMicrophoneList();
        recordAudio.clip = Microphone.Start(null, true, loopTIme, samplingRate);
        this.mainDevice.log("" + recordAudio.clip.length);

        ShowMicrophoneDeviceCaps(null);


        // speex
        speexEncoder = new SpeexEncoder(BandMode.Narrow);
        speexDecoder = new SpeexDecoder(BandMode.Narrow);
        recordSampleSize = samplingRate / (targetSamplingRate / targetSamplingSize);
        sampleBuffer = new float[recordSampleSize];

        this.mainDevice.log("---RecordStart---");
    }

    public void RecordEnd () {
        if (Microphone.IsRecording(null)) {
            this.mainDevice.log("---RecordEnd---");
            Microphone.End(null);

            byte[] samples = null;
            float[] fsamples = null;

            if (usingSpeex) {
                byte[] encodeData = this.encodeBufList.ToArray(typeof(byte)) as byte[];
                short[] decodedFrame = new short[totalSampleSize]; // should be the same number of samples as on the capturing side
                speexDecoder.Decode(encodeData, 0, encodeData.Length, decodedFrame, 0, false);
                fsamples = ToFloatArray(decodedFrame);

            } else {
                samples = this.encodeBufList.ToArray(typeof(byte)) as byte[];
                fsamples = ToFloatArray(samples);
            }
            
            Play(fsamples, recordAudio.clip.channels);
        }

    }

    float GetAveragedVolume () {
        float[] data = new float[256];
        float a = 0;
        recordAudio.GetOutputData(data, 0);
        foreach (float s in data) {
            a += Mathf.Abs(s);
        }

        return a / 256;
    }

   void ShowMicrophoneList () {
        this.mainDevice.log("Device List:");
        foreach (string device in Microphone.devices) {
            this.mainDevice.log(device);
        }
    }

    void ShowMicrophoneDeviceCaps(string deviceName) {
        int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
        this.mainDevice.log("max freq : " + maxFreq);
    }

    byte[] ToByteArray (float[] floatArray) {
        int len = floatArray.Length * 4;
        byte[] byteArray = new byte[len];
        int count = 0;
        foreach (float f in floatArray) {
            byte[] data = System.BitConverter.GetBytes(f);
            System.Array.Copy(data, 0, byteArray, count, 4);
            count += 4;
        }
        return byteArray;
    }

    byte[] ToByteArray (short[] shortArray) {
        int len = shortArray.Length * 2;
        byte[] byteArray = new byte[len];
        int count = 0;
        foreach (short s in shortArray) {
            byte[] data = System.BitConverter.GetBytes(s);
            System.Array.Copy(data, 0, byteArray, count, 2);
            count += 2;
        }
        return byteArray;
    }

    public float[] ToFloatArray (byte[] byteArray) {
        int len = byteArray.Length / 4;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i += 4) {
            floatArray[i / 4] = System.BitConverter.ToSingle(byteArray, i);
        }
        return floatArray;
    }

    public short[] ToShortArray (byte[] byteArray) {
        int len = byteArray.Length / 2;
        short[] shortArray = new short[len];
        for (int i = 0; i < byteArray.Length; i += 2) {
            shortArray[i / 2] = System.BitConverter.ToInt16(byteArray, i);
        }
        return shortArray;
    }
    

    public short[] ToShortArray (float[] floatArray) {
        int len = floatArray.Length;
        short[] shortArray = new short[len];
        for (int i = 0; i < floatArray.Length; ++i) {
            shortArray[i] = (short)Mathf.Clamp((int)(floatArray[i] * 32767.0f), short.MinValue, short.MaxValue);
        }
        return shortArray;
    }

    public float[] ToFloatArray (short[] shortArray) {
        int len = shortArray.Length;
        float[] floatArray = new float[len];
        for (int i = 0; i < shortArray.Length; ++i) {
            floatArray[i] = shortArray[i] / (float)short.MaxValue;
        }

        return floatArray;
    }

    public void Play (float[] fSamples, int chan) {
        playAudio.clip = AudioClip.Create("test", fSamples.Length, chan, targetSamplingRate, false);
        playAudio.clip.SetData(fSamples, 0);
        if (!playAudio.isPlaying) {
            playAudio.Play();
        }
    }

    public void FileWrite(string filepath, ArrayList list) {
        FileStream fs = new FileStream(filepath, FileMode.Create);
        BinaryWriter sw = new BinaryWriter(fs);
        sw.Write(list.ToArray(typeof(byte)) as byte[]);
        sw.Flush();
        sw.Close();
        fs.Close();
    }

}
