﻿using FFmpeg.AutoGen;
using System;
using System.IO;

namespace FFMPEG_TUTORIAL2
{
    public class FFmpegBinariesHelper
    {
        public static void RegisterFFmpegBinaries()
        {
            var current = Environment.CurrentDirectory;
            var probe = Path.Combine("FFmpeg", "bin", Environment.Is64BitProcess ? "x64" : "x86");
            while (current != null)
            {
                var ffmpegBinaryPath = Path.Combine(current, probe);
                if (Directory.Exists(ffmpegBinaryPath))
                {
                    Console.WriteLine($"FFmpeg binaries found in: {ffmpegBinaryPath}");
                    ffmpeg.RootPath = ffmpegBinaryPath;
                    return;
                }

                current = Directory.GetParent(current)?.FullName;
            }
        }
    }
}
