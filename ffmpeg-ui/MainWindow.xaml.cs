using FFmpeg.NET;
using FFmpeg.NET.Enums;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Media.Streaming.Adaptive;

namespace ffmpeg_ui;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static MainWindow instance;
    public MainWindow()
    {
        instance = this;
        InitializeComponent();
        checkExportConditions();
    }
    bool inputFile = false, outputFile = false;
    // Arguments
    int accelType = 0;
    int codecTypeID = 0;
    float bitrate = 5;
    private void Window_ActualThemeChanged(object sender, RoutedEventArgs e)
    {
        if (ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
        {
            Background = new SolidColorBrush(Color.FromArgb(0xAA, 0x0, 0x0, 0x0));
        }
        else
        {
            Background = new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
        }
    }

    private void changeArgs_Click(object sender, RoutedEventArgs e)
    {
        CustomizeArguments ca = new CustomizeArguments();
        ca.ShowAsync();
    }

    private void openFile_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "Video Files|" +
                     "*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.wmv;*.mpeg;*.mpg;" +
                     "*.m4v;*.3gp;*.ts;*.mts;*.m2ts;*.ogv;*.vob;*.rm;*.rmvb;*.asf;" +
                     "*.f4v;*.divx;*.dv;*.amv;*.nsv;*.mjpeg;*.mjpg|" +
                     "All Files|*.*";
        if (ofd.ShowDialog() == true)
        {
            inPath.Text = ofd.FileName;
        }
        inputFile = true;
        checkExportConditions();
    }

    private void saveFile_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog sfd = new SaveFileDialog();
        sfd.Filter = "MP4 Video (*.mp4)|*.mp4|" +
                     "MKV Video (*.mkv)|*.mkv|" +
                     "WebM Video (*.webm)|*.webm|" +
                     "AVI Video (*.avi)|*.avi|" +
                     "MOV Video (*.mov)|*.mov|" +
                     "FLV Video (*.flv)|*.flv|" +
                     "WMV Video (*.wmv)|*.wmv|" +
                     "MPEG Video (*.mpeg;*.mpg)|*.mpeg;*.mpg|" +
                     "3GP Video (*.3gp)|*.3gp|" +
                     "TS Video (*.ts)|*.ts|" +
                     "OGV Video (*.ogv)|*.ogv|" +
                     "All Video Files (*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.wmv;*.mpeg;*.mpg;*.3gp;*.ts;*.ogv)|" +
                     "*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.wmv;*.mpeg;*.mpg;*.3gp;*.ts;*.ogv";
        sfd.DefaultExt = "mp4";
        sfd.FilterIndex = 1;
        if (sfd.ShowDialog() == true)
        {
            outPath.Text = sfd.FileName;
        }
        outputFile = true;
        checkExportConditions();
    }
    public void checkExportConditions()
    {
        if(inputFile && outputFile)
        {
            exportButton.IsEnabled = true;
        }
        else
        {
            exportButton.IsEnabled = false;
        }
    }

    private async void exportButton_Click(object sender, RoutedEventArgs e)
    {
        string input = inPath.Text.Replace("\"", "");
        string output = outPath.Text.Replace("\"", "");
        StringBuilder options = new StringBuilder();
        if (accelType == 1)
            options.Append("Acceleration: NVIDIA");
        else if (accelType == 2)
            options.Append("Acceleration: AMD");
        else if (accelType == 3)
            options.Append("Acceleration: INTEL");
        else
            options.Append("Acceleration: CPU (Software)");
        options.Append("\n");
        switch (codecTypeID)
        {
            case 0:
                options.Append("Codec Type: H264");
                break;
            case 1:
                options.Append("Codec Type: HEVC");
                break;
        }
        options.Append("\n");
        options.Append("Bitrate: " + bitrate + "M\n");
        options.Append("Input File: " + inPath.Text);
        options.Append("\nOutput File: " + outPath.Text);
        ContentDialog confirmDialog = new ContentDialog()
            {
                Title = "Confirmation",
                Content = "Are you sure you want to do the operation with these options?\n\n" + options,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };
        if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var tokenSource = new CancellationTokenSource();
            var inputFile = new InputFile(input);
            var outputFile = new OutputFile(output);
            Engine ffmpeg = new Engine(Directory.GetCurrentDirectory() + "\\ffmpeg.exe");
            var conversionOptions = new ConversionOptions
            {
                HWAccel = HWAccel.None
            };
            switch (accelType)
            {
                case 0:
                    conversionOptions = new ConversionOptions
                    {
                        HWAccel = HWAccel.None
                    };
                    break;
                case 1:
                    conversionOptions = new ConversionOptions
                    {
                        HWAccel = HWAccel.cuda
                    };
                    break;
                case 2:
                    conversionOptions = new ConversionOptions
                    {
                        HWAccel = HWAccel.d3d11va
                    };
                    break;
                case 3:
                    conversionOptions = new ConversionOptions
                    {
                        HWAccel = HWAccel.qsv
                    };
                    break;
            }
            switch (codecTypeID)
            {
                case 0:
                    if (accelType == 1)
                        conversionOptions.VideoCodec = VideoCodec.nvenc_h264;
                    else if (accelType == 2)
                        conversionOptions.VideoCodec = VideoCodec.h264_amf;
                    else if (accelType == 3)
                        conversionOptions.VideoCodec = VideoCodec.h264_qsv;
                    else
                        conversionOptions.VideoCodec = VideoCodec.libx264;
                    break;
                case 1:
                    if (accelType == 1)
                        conversionOptions.VideoCodec = VideoCodec.nvenc_hevc;
                    else if (accelType == 2)
                        conversionOptions.VideoCodec = VideoCodec.hevc_amf;
                    else if (accelType == 3)
                        conversionOptions.VideoCodec = VideoCodec.hevc_qsv;
                    else
                        conversionOptions.VideoCodec = VideoCodec.libx265;
                    break;
            }
            if(bitrate == 1)
                conversionOptions.VideoBitRate = 700;
            else if(bitrate < 1)
                conversionOptions.VideoBitRate = (int)(bitrate * 1000) - 200;
            else
                conversionOptions.VideoBitRate = (int)(bitrate * 1000) - 1000;
            if (conversionOptions.VideoBitRate < 0)
                conversionOptions.VideoBitRate = 100;
            ffmpeg.Progress += Ffmpeg_Progress;
            ffmpeg.Complete += Ffmpeg_Complete;
            ffmpeg.Error += Ffmpeg_Error;
            showProgress();
            new Thread(async () =>
            {
                await ffmpeg.ConvertAsync(inputFile, outputFile, conversionOptions, tokenSource.Token);
            }).Start();
        }
    }

    private void Ffmpeg_Error(object? sender, FFmpeg.NET.Events.ConversionErrorEventArgs e)
    {
        this.Dispatcher.Invoke(async () =>
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Oh nooo!",
                Content = "The current task failed to execute.\nTry troubleshooting by reading the exception below or contact the developer\n\n" + e.Exception.Message,
                PrimaryButtonText = "Okay qwq",
                IsPrimaryButtonEnabled = true
            };
            if(await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                closePopupUI();
            }
        });
    }

    private void Ffmpeg_Complete(object? sender, FFmpeg.NET.Events.ConversionCompleteEventArgs e)
    {
        status.Dispatcher.Invoke(() =>
        {
            status.Content = status.Content + "\nFinished!";
            popupTitle.Content = "Conversion Finished!";
            progress.Value = 1;
            closePopup.IsEnabled = true;
        });
    }

    private void Ffmpeg_Progress(object? sender, FFmpeg.NET.Events.ConversionProgressEventArgs e)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(string.Format("File: {0} => {1}\n", e.Input.Name, e.Output.Name));
        sb.Append(string.Format("Bitrate: {0}\n", e.Bitrate));
        sb.Append(string.Format("FPS: {0}\n", e.Fps));
        sb.Append(string.Format("Processed Frame: {0}\n", e.Frame));
        sb.Append(string.Format("TotalDuration: {0}\n", (int)e.TotalDuration.TotalSeconds));
        if (accelType == 1)
            sb.Append("Using NVIDIA Acceleration");
        else if (accelType == 2)
            sb.Append("Using AMD Acceleration");
        else if (accelType == 3)
            sb.Append("Using INTEL Acceleration");
        else
            sb.Append("Using CPU Acceleration");
        sb.Append("\n");
        if (codecTypeID == 0)
            sb.Append("Codec: H264");
        else if (codecTypeID == 1)
            sb.Append("Codec: HEVC");
        status.Dispatcher.Invoke(() =>
        {
            progress.Value = (e.ProcessedDuration / e.TotalDuration) % 100;
            status.Content = sb;
        });
    }

    private void closePopup_Click(object sender, RoutedEventArgs e)
    {
        closePopupUI();
    }
    private void closePopupUI()
    {
        main.Visibility = Visibility.Visible;
        BlurEffect be = new BlurEffect();
        be.RenderingBias = RenderingBias.Performance;
        main.Effect = be;
        ThicknessAnimation ta = new ThicknessAnimation
        {
            From = new Thickness(0, 0, 0, 0),
            To = new Thickness(0, this.Height, 0, 0),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        DoubleAnimation da = new DoubleAnimation
        {
            From = 20,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        DoubleAnimation daOpacity = new DoubleAnimation
        {
            From = 0.3,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        be.BeginAnimation(BlurEffect.RadiusProperty, da);
        main.BeginAnimation(Grid.OpacityProperty, daOpacity);
        main.IsEnabled = true;
        popupOnProgress.BeginAnimation(Grid.MarginProperty, ta);
    }
    public static void changeSettings(int accelType, int codecType, float specifiedBitrate)
    {
        instance.accelType = accelType;
        instance.codecTypeID = codecType;
        instance.bitrate = specifiedBitrate;
    }

    private void inPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        inputFile = true;
        checkExportConditions();
    }

    private void outPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        outputFile = true;
        checkExportConditions();
    }

    public void showProgress()
    {
        BlurEffect be = new BlurEffect();
        be.RenderingBias = RenderingBias.Performance;
        main.Effect = be;
        popupOnProgress.Visibility = Visibility.Visible;
        closePopup.IsEnabled = false;
        ThicknessAnimation ta = new ThicknessAnimation
        {
            From = new Thickness(0, this.Height, 0, 0),
            To = new Thickness(0, 0, 0, 0),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation da = new DoubleAnimation
        {
            From = 0,
            To = 20,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation daOpacity = new DoubleAnimation
        {
            From = 1,
            To = 0.3,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        be.BeginAnimation(BlurEffect.RadiusProperty, da);
        main.BeginAnimation(Grid.OpacityProperty, daOpacity);
        main.IsEnabled = false;
        popupOnProgress.BeginAnimation(Grid.MarginProperty, ta);
    }
}