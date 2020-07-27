namespace FFMPEG_TUTORIAL1
{
    using FFmpeg.AutoGen;
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;

    class Program
    {
        static unsafe void Main(string[] args)
        {
            if (args.Length < 0)
            {
                Console.WriteLine($"Please enter a paramter for input file name!");
                Console.WriteLine($"ex ) {System.Reflection.Assembly.GetExecutingAssembly().GetName().Name} test.mp4");
                return;
            }

            if (!System.IO.File.Exists(args[0]))
            {
                Console.WriteLine($"The file, {args[0]} doesn't exist!");
                return;
            }


            FFmpegBinariesHelper.RegisterFFmpegBinaries();
            Console.WriteLine($"FFmpeg version info: {ffmpeg.av_version_info()}");

            Console.WriteLine($"The file, {args[0]} will be used.");
            var filePath = args[0];

            AVFormatContext* pFormatCtx = ffmpeg.avformat_alloc_context();

            if (ffmpeg.avformat_open_input(&pFormatCtx, filePath, null,null) != 0)
            {
                Console.WriteLine($"The file, {filePath} can't be opened!");
                return;
            }

            if(ffmpeg.avformat_find_stream_info(pFormatCtx, null) < 0)
            {
                Console.WriteLine($"Can't find streams of {filePath}!");
                return;
            }

            for(int i=0; i<pFormatCtx->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatCtx, i, filePath, 0);
            }

            AVCodec* pCodec = null;
            int videoCodecIndex = ffmpeg.av_find_best_stream(pFormatCtx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &pCodec, 0);

            if (videoCodecIndex == -1)
            {
                Console.WriteLine($"Can't find a video stream of {filePath}!");
                return;
            }

            AVCodecContext* pCodecCtx = ffmpeg.avcodec_alloc_context3(pCodec);

            if (pCodecCtx == null)
            {
                Console.WriteLine($"Can't find a video codec context of {filePath}!");
                return;
            }

            if(ffmpeg.avcodec_parameters_to_context(pCodecCtx, pFormatCtx->streams[videoCodecIndex] ->codecpar) < 0)
            {
                Console.WriteLine($"Can't set codec params to its context!");
                return;
            }

            var codecName = ffmpeg.avcodec_get_name(pCodec->id);

            if (ffmpeg.avcodec_open2(pCodecCtx, pCodec, null) < 0)
            {
                Console.WriteLine($"Can't open {filePath} with codec, {codecName}");
                return;
            }

            var frameSize = new Size(pCodecCtx->width, pCodecCtx->height);
            var pixelFormat = pCodecCtx->pix_fmt;

            Console.WriteLine($"Succeeded to open the file, {filePath}({codecName}, {frameSize.Width}X{frameSize.Height}, {pixelFormat})");

            AVFrame* pFrame = ffmpeg.av_frame_alloc();
            if (pFrame == null)
            {
                Console.WriteLine($"Failed to allocate frame");
                return;
            }


            AVFrame* pFrameRgb = ffmpeg.av_frame_alloc();
            if (pFrameRgb == null)
            {
                Console.WriteLine($"Failed to allocate RGB frame");
                return;
            }

            int numBytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_RGB24, pCodecCtx->width, pCodecCtx->height,32);


            if (numBytes <= 0)
            {
                Console.WriteLine($"Can't get proper buffer size.");
                return;
            }


            Console.ReadKey();
        }
    }
}


