using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;

namespace SoundPlayer.Services
{
    public class MidiPlayer
    {
        public MidiPlayer()
        {
            
        }

        public void PlayNote(int note, int durationMs)
        {
            double frequency = NoteToFrequency(note);
            byte[] waveData = GenerateTone(frequency, durationMs);
            using (var ms = new MemoryStream())
            {
                WriteWavHeader(ms, waveData.Length);
                ms.Write(waveData, 0, waveData.Length);
                ms.Position = 0;
                WaveStream waveStream = new WaveFileReader(ms);
                waveStream.Position = 0;
                WaveOutEvent waveOut = new WaveOutEvent();
                waveOut.Init(waveStream);
                waveOut.Play();

                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(10);
                }
            }
        }

        public static byte[] GenerateTone(double frequency, int durationMs, int sampleRate = 44100)
        {
            int samples = (int)((durationMs / 1000.0) * sampleRate);
            byte[] buffer = new byte[samples * 2]; // 16-bit PCM

            for (int i = 0; i < samples; i++)
            {
                short value = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * short.MaxValue);
                buffer[i * 2] = (byte)(value & 0xFF);
                buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }

            return buffer;
        }

        public static void WriteWavHeader(Stream stream, int dataLength, int sampleRate = 44100)
        {
            int byteRate = sampleRate * 2; // 16-bit mono = 2 bytes per sample
            int blockAlign = 2;
            int bitsPerSample = 16;
            int subchunk2Size = dataLength;
            int chunkSize = 36 + subchunk2Size;

            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
            {
                writer.Write("RIFF".ToCharArray());              // Chunk ID
                writer.Write(chunkSize);                         // Chunk Size
                writer.Write("WAVE".ToCharArray());              // Format
                writer.Write("fmt ".ToCharArray());              // Subchunk1 ID
                writer.Write(16);                                // Subchunk1 Size (PCM)
                writer.Write((short)1);                          // Audio Format (1 = PCM)
                writer.Write((short)1);                          // Num Channels (1 = mono)
                writer.Write(sampleRate);                        // Sample Rate
                writer.Write(byteRate);                          // Byte Rate
                writer.Write((short)blockAlign);                 // Block Align
                writer.Write((short)bitsPerSample);              // Bits per Sample
                writer.Write("data".ToCharArray());              // Subchunk2 ID
                writer.Write(subchunk2Size);                     // Subchunk2 Size
            }
        }

        public static double NoteToFrequency(int note)
        {
            int midiNote = note + 12;
            return 440.0 * Math.Pow(2, (midiNote - 69) / 12.0);
        }
    }
}
