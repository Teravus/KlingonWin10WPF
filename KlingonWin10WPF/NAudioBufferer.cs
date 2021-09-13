using System;
using NAudio.Wave;
using LibVLCSharp.Shared;
using System.Runtime.InteropServices;

namespace KlingonWin10WPF
{
    public class NAudioBufferer
    {
        WaveFormat waveFormat;
        
        BufferedWaveProvider bufferedWaveProvider;
        WaveOutEvent outputDevice;
        private volatile bool playing;

        public NAudioBufferer(MediaPlayer mediaPlayer)
        {
            outputDevice = new WaveOutEvent();
            waveFormat = new WaveFormat(44100, 16, 1);
            //waveFileWriter = new WaveFileWriter("sound.wav", waveFormat);
            bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(10);
            outputDevice.Init(bufferedWaveProvider);
            mediaPlayer.SetAudioFormatCallback(AudioSetup, AudioCleanup);
            mediaPlayer.SetAudioCallbacks(PlayAudio, PauseAudio, ResumeAudio, FlushAudio, DrainAudio);
        }

        void PlayAudio(IntPtr data, IntPtr samples, uint count, long pts)
        {
            int bytes = (int)count * 2; // (16 bit, 1 channel)
            var buffer = new byte[bytes];
            Marshal.Copy(samples, buffer, 0, bytes);

            bufferedWaveProvider.AddSamples(buffer, 0, bytes);
            if (bufferedWaveProvider.BufferedDuration > TimeSpan.FromSeconds(4))
            {
                playing = true;
                outputDevice.Play();
            }
        }

        int AudioSetup(ref IntPtr opaque, ref IntPtr format, ref uint rate, ref uint channels)
        {
            channels = (uint)waveFormat.Channels;
            rate = (uint)waveFormat.SampleRate;
            return 0;
        }

        void DrainAudio(IntPtr data)
        {
            //writer.Flush();
        }

        void FlushAudio(IntPtr data, long pts)
        {
            // writer.Flush();
            bufferedWaveProvider.ClearBuffer();
        }

        void ResumeAudio(IntPtr data, long pts)
        {
            //playing = true;
            //outputDevice.Play();
        }

        void PauseAudio(IntPtr data, long pts)
        {
            playing = false;
            outputDevice.Pause();
        }

        void AudioCleanup(IntPtr opaque) { }
    }
}
