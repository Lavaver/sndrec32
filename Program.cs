using NAudio.Wave;
using System;
using LibVLCSharp.Shared;
using System.Diagnostics;

namespace sndrec32
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("检查 .NET 桌面运行时版本。");
            // 检查.NET运行时版本是否符合要求
            Version requiredVersion = new Version(8, 0);

            if (Environment.Version < requiredVersion)
            {
                Console.WriteLine("错误：当前 .NET 运行时版本低于所需版本，或未安装相应运行时，请升级或安装 .NET 8.0 及以上版本。");
                Console.WriteLine("至 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 安装 .NET 8.0 桌面运行时");
                Console.WriteLine("针对于开发者，你应该确保安装了 SDK 8.0 及以上版本。");
                return;
            }
            else
            {
                Console.WriteLine("运行时版本满足该软件运行需要，继续。");
                Console.WriteLine("---------------------------------");
            }

            Console.WriteLine("初始化内核，请稍候...");
            Core.Initialize();
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            Console.WriteLine("内核初始化完成。");

            // 检测是否处于调试模式
            if (Debugger.IsAttached)
            {
                Console.ForegroundColor = ConsoleColor.Yellow; // 将字体颜色设置为黄色
                Console.WriteLine("警告：您当前处于调试模式。");
                ShowDebugMenu(); // 显示调试菜单
                Console.ResetColor(); // 重置控制台颜色
                return; // 在展示调试菜单后退出程序或继续根据用户输入进行调试操作
            }

            if (args.Length < 1)
            {
                Console.WriteLine("用法：snderc32 <-play/-rec/-radio/-log> [音频地址/录音保存路径/在线电台频道(CNR1,CNR2,CNR3,CGTN)或指定广播地址]");
                return;
            }

            string command = args[0];

            if (command != "-play" && command != "-rec" && command !="-radio" && command !="-log")
            {
                Console.WriteLine("未知命令。请使用 -play 参数播放音频，-rec 录音，-radio 聆听在线广播或 -log 查阅更新日志。");
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
            else if (args.Length > 1 && args[0] == "-radio")
            {
                string channel = args[1]; // 获取频道信息
                PlayChinaSound(channel);
            }
            else if (command == "-log")
            {
                Showupdatelog(args);
            }

        }

        static void PlayMusic(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("请输入文件地址以播放音频。");
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

                    Console.WriteLine("正在播放。按任意键停止并退出程序。");
                    Console.ReadKey();

                    outputDevice.Stop();
                }
            }
            else
            {
                Console.WriteLine("目前仅支持 .wav, .mp3, .ogg, 与 .flac 格式的音频文件");
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
                    Console.WriteLine("未定义录音名称或后缀无效。已将文件名重定向为 rec44.wav 。");
                }
            }

            using (var waveIn = new WaveInEvent())
            {
                waveIn.WaveFormat = new WaveFormat(44100, 1); // 设置音频格式为44.1kHz采样率

                var writer = new WaveFileWriter(saveLocation, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, e) =>
                {
                    writer.Write(e.Buffer, 0, e.BytesRecorded);
                };

                waveIn.StartRecording();

                Console.WriteLine("正在录音。按任意键停止录音并保存关闭软件。");
                Console.ReadKey();

                waveIn.StopRecording();
                writer.Dispose();
            }

            Console.WriteLine("您的录音已存储至：" + saveLocation);
        }

        static void PlayChinaSound(string channel)
        {
                string url = GetChinaSoundUrlForChannel(channel); // 根据频道获取相应的 mms 协议网络广播地址

                using (var libVLC = new LibVLC())
                {
                    using (var media = new Media(libVLC, new Uri(url)))
                    {
                        using (var mediaPlayer = new MediaPlayer(media))
                        {
                            mediaPlayer.Play();

                            Console.WriteLine($"目前正在播放来自 {channel} 频道的广播。按任意键停止广播并关闭软件。");
                            Console.ReadKey();

                            mediaPlayer.Stop();
                        }
                    }
                }
            
        }

        static string GetChinaSoundUrlForChannel(string channel)
        {
            switch (channel)
            {
                case "CNR1":
                    return "https://live-play.cctvnews.cctv.com/cctv/zgzs192.m3u8";
                case "CNR2":
                    return "http://ngcdn006.cnr.cn/live/szzs/index.m3u8";
                case "CNR3":
                    return "http://ngcdn016.cnr.cn/live/gsgljtgb/index.m3u8";
                case "CGTN":
                    return "http://sk.cri.cn/am846.m3u8";
                default:
                    return channel; // 如果频道信息不匹配，则使用指定地址播放
            }
        }

        static void Showupdatelog(string[] args)
        {
            Console.WriteLine("sndrec32 - 一款微软组件复兴项目 发行版 v2.2");
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("基于 NAudio 及 libVLC 库技术架构");
            Console.WriteLine("请支持自由软件事业的开发，谢谢！");
            Console.WriteLine("若你是通过购买方式获得的该软件或所谓“破解版”，你应该向商家退款并维权。");
            Console.WriteLine("仓库地址：https://github.com/lavaver/sndrec32");
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("更新内容：");
            Console.WriteLine("- 常规更新。修复了包括抛出异常无法得到有效反馈在内的 Bug。");
            Console.WriteLine("- 为开发人员新增调试模式菜单。");
            Console.WriteLine("- 有关该版本的详细信息，请参阅 https://github.com/lavaver/sndrec32/releases");
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("OOps！程序居然以及其不优雅的姿态蓝屏了！");
            Console.WriteLine("这对于开发者运动会上是极为重大的失误！这个程序可能因此被裁判红牌罚下！");
            Console.WriteLine("我们将输出有关引起该错误的日志，稍后你可以重新启动程序或提交 Issues。");
            Console.WriteLine((e.ExceptionObject as Exception).Message);
            Environment.Exit(1);
        }

        static void ShowDebugMenu()
        {
            Console.WriteLine("调试菜单");
            Console.WriteLine("-------------------");
            Console.WriteLine("[1] 音乐播放");
            Console.WriteLine("[2] 广播");
            Console.WriteLine("[3] 手动触发异常");
            Console.WriteLine("[4] 退出程序");

            bool keepRunning = true; // 控制循环的布尔变量

            while (keepRunning)
            {

                Console.WriteLine("请输入选项：");
                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        // 调用音乐播放函数，此处需要实现逻辑以获取音乐文件路径
                        Console.WriteLine("请输入音乐文件路径：");
                        string musicPath = Console.ReadLine();
                        PlayMusic(musicPath);
                        break;
                    case "2":
                        // 直接播放 CGTN
                        Console.WriteLine("播放预设广播频道...");
                        PlayChinaSound("CGTN");
                        break;
                    case "3":
                        Console.WriteLine("手动触发异常...");
                        throw new Exception("异常已被手动抛出。");
                    case "4":
                        Console.WriteLine("退出调试...");
                        keepRunning = false; // 更新控制变量以退出循环
                        break;
                    default:
                        Console.WriteLine("无效的选项。");
                        break;
                }
            }
        }
    }
}