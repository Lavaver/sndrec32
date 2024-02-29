using NAudio.Wave;
using System;

namespace MusicPlayer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: snderc32 <-play/-rec> [music_file_path/save_location]");
                return;
            }

            string command = args[0];

            if (command != "-play" && command != "-rec")
            {
                Console.WriteLine("Invalid command. Please use -play for playing or -rec for recording.");
                return;
            }

            string filePath = "";
            if (args.Length >= 2)
            {
                filePath = args[1];
            }

            if (command == "-play")
            {
                PlayMusic(filePath);
            }
            else if (command == "-rec")
            {
                RecordAudio(filePath);
            }
        }

        static void PlayMusic(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("Please provide a valid music file path to play.");
                return;
            }

            string extension = System.IO.Path.GetExtension(filePath).ToLower();

            if (extension == ".wav" || extension == ".mp3" || extension == ".ogg" || extension == ".flac")
            {
                using (var audioFile = new AudioFileReader(filePath))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();

                    Console.WriteLine("Playing music. Press any key to stop.");
                    Console.ReadKey();

                    outputDevice.Stop();
                }
            }
            else
            {
                Console.WriteLine("Unsupported file format. Only .wav, .mp3, .ogg, and .flac are supported.");
            }
        }

        static void RecordAudio(string saveLocation)
        {
            if (string.IsNullOrEmpty(saveLocation))
            {
                saveLocation = "rec44.wav"; // 如果未指定保存路径，则默认使用 "rec44.wav"
            }
            else
            {
                string extension = System.IO.Path.GetExtension(saveLocation).ToLower();
                if (extension != ".wav")
                {
                    saveLocation = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(saveLocation), "rec44.wav");
                    Console.WriteLine("Invalid file extension. Defaulting to rec44.wav as save location.");
                }
            }

            using (var waveIn = new WaveInEvent())
            {
                waveIn.WaveFormat = new WaveFormat(44100, 1); // 设置音频格式为44.1kHz采样率，单声道

                var writer = new WaveFileWriter(saveLocation, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, e) =>
                {
                    writer.Write(e.Buffer, 0, e.BytesRecorded);
                };

                waveIn.StartRecording();

                Console.WriteLine("Recording. Press any key to stop and save.");
                Console.ReadKey();

                waveIn.StopRecording();
                writer.Dispose();
            }

            Console.WriteLine("Recording saved to: " + saveLocation);
        }
    }
}