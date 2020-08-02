using System;

namespace FFMPEG_TUTORIAL2
{
    class Program
    {
        static void Main(string[] args)
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

            if (ffmpeg.avformat_open_input(&pFormatCtx, filePath, null, null) != 0)
            {
                Console.WriteLine($"The file, {filePath} can't be opened!");
                return;
            }

            if (ffmpeg.avformat_find_stream_info(pFormatCtx, null) < 0)
            {
                Console.WriteLine($"Can't find streams of {filePath}!");
                return;
            }

            for (int i = 0; i < pFormatCtx->nb_streams; i++)
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

            if (ffmpeg.avcodec_parameters_to_context(pCodecCtx, pFormatCtx->streams[videoCodecIndex]->codecpar) < 0)
            {
                Console.WriteLine($"Can't set codec params to its context!");
                return;
            }

            if (ffmpeg.avcodec_open2(pCodecCtx, pCodec, null) < 0)
            {
                Console.WriteLine($"Can't open {filePath} with codec, {ffmpeg.avcodec_get_name(pCodec->id)}");
                return;
            }

            var codecName = ffmpeg.avcodec_get_name(pCodec->id);
            var frameSize = new Size(pCodecCtx->width, pCodecCtx->height);
            var pixelFormat = pCodecCtx->pix_fmt;

            Console.WriteLine($"Succeeded to open the file, {filePath}({codecName}, {frameSize.Width}X{frameSize.Height}, {pixelFormat})");



            AVFrame* pFrame = ffmpeg.av_frame_alloc();
            if (pFrame == null)
            {
                Console.WriteLine($"Failed to allocate frame");
                return;
            }

            var sourceSize = frameSize;
            var sourcePixelForamt = pixelFormat;
            var destSize = sourceSize;
            var destPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;

            var convertContext = ffmpeg.sws_getContext(sourceSize.Width, sourceSize.Height,
                                                   sourcePixelForamt,
                                                   destSize.Width, destSize.Height,
                                                   destPixelFormat,
                                                   ffmpeg.SWS_FAST_BILINEAR,
                                                   null,
                                                   null, null);


            int numBytes = ffmpeg.av_image_get_buffer_size(destPixelFormat, destSize.Width, destSize.Height, 1);

            if (numBytes <= 0)
            {
                Console.WriteLine($"Can't get proper buffer size.");
                return;
            }


            var buffer = Marshal.AllocHGlobal(numBytes);
            var destData = new byte_ptrArray4();
            var destLinesize = new int_array4();


            var sdvb = ffmpeg.av_image_fill_arrays(ref destData, ref destLinesize, null, destPixelFormat, destSize.Width, destSize.Height, 1);

            ffmpeg.av_image_fill_arrays(ref destData, ref destLinesize, (byte*)buffer, destPixelFormat, destSize.Width, destSize.Height, 1);

            AVFrame* pFrameRgb = ffmpeg.av_frame_alloc();
            if (pFrameRgb == null)
            {
                Console.WriteLine($"Failed to allocate RGB frame");
                return;
            }

            AVPacket packet;
            int frameNumber = 0;
            int error = 0;
            do
            {
                try
                {
                    do
                    {
                        error = ffmpeg.av_read_frame(pFormatCtx, &packet);
                    } while (packet.stream_index != videoCodecIndex);

                    ffmpeg.avcodec_send_packet(pCodecCtx, &packet);
                }
                finally
                {
                    ffmpeg.av_packet_unref(&packet);
                }

                error = ffmpeg.avcodec_receive_frame(pCodecCtx, pFrame);

                var result = ffmpeg.sws_scale(convertContext, pFrame->data, pFrame->linesize, 0, pCodecCtx->height, destData, destLinesize);
                var data = new byte_ptrArray8();
                data.UpdateFrom(destData);
                var lineSize = new int_array8();
                lineSize.UpdateFrom(destLinesize);

                var destFrame = new AVFrame() { data = data, linesize = lineSize, width = destSize.Width, height = destSize.Height };


                using (var bitmap = new Bitmap(destFrame.width, destFrame.height, destFrame.linesize[0], PixelFormat.Format24bppRgb, (IntPtr)destFrame.data[0]))
                    bitmap.Save($"frame.{frameNumber:D8}.jpg", ImageFormat.Jpeg);
                Console.WriteLine($"frame: {frameNumber}");
                frameNumber++;

            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

            Console.ReadKey();
        }
    }
}
