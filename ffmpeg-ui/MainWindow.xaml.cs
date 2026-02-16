using FFMpegCore;
using FFMpegCore.Enums;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using Microsoft.Win32;
using Octokit;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
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
using Windows.Media.Protection.PlayReady;
using Windows.Media.Streaming.Adaptive;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using Path = System.IO.Path;

namespace ffmpeg_ui;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static MainWindow instance;
    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
    public MainWindow()
    {
        instance = this;
        InitializeComponent();
        initSettings();
        checkExportConditions();
        checkFFmpeg();
        checkUpdate();
    }

    private void initSettings()
    {
        key.CreateSubKey("ffmpeg-ui");
        key = key.OpenSubKey("ffmpeg-ui", true);

        //key.SetValue("mica", true);
        if ((int)key.GetValue("mica", 0) == 1)
        {
            micaToggle.IsChecked = true;
            WindowHelper.SetSystemBackdropType(this, BackdropType.Tabbed);
            WindowHelper.SetApplyBackground(this, false);
        }

    }

    bool inputFile = false, outputFile = false;
    string ffmpegPath = "";
    static string tagname = "v1.0";
    // Arguments
    int accelType = 0;
    int codecTypeID = 0;
    int bitrate = 5;
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
    private async void checkUpdate()
    {
        var github = new GitHubClient(new ProductHeaderValue("ffmpeg-ui"));
        var releases = github.Repository.Release.GetLatest("caramellizedd", "ffmpeg-ui");
        var latest = releases.Result;
        if (latest.TagName != tagname)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Update Notice",
                Content = "An update is available. Click the update button to update!",
                PrimaryButtonText = "Update",
                SecondaryButtonText = "Nah",
                IsPrimaryButtonEnabled = true,
                IsSecondaryButtonEnabled = true
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = "https://github.com/caramellizedd/ffmpeg-ui/releases/latest";
                Process.Start(psi);
            }
        }
    }
    private void checkFFmpeg(bool forceUpdate = false)
    {
        showProgress(1);
        new Thread(() =>
        {
            this.Dispatcher.Invoke(() =>
            {
                dragDropDialog.IsEnabled = false;
            });
            bool ffmpegInstalled = File.Exists(Environment.CurrentDirectory + "\\bin\\ffmpeg.exe");
            string latestFFmpeg = "https://github.com/btbn/ffmpeg-builds/releases/latest/download/ffmpeg-master-latest-win64-gpl-shared.zip";

            string zipPath = Path.GetTempFileName();
            string extractPath = Path.GetTempPath() + "ffmpegtemp";

            if (!ffmpegInstalled || forceUpdate)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(new Uri(latestFFmpeg), zipPath);
                    ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                }
                //move ffmpeg-master-latest-win64-gpl-shared\bin\ from extracted path to main directory
                if (Directory.Exists(Environment.CurrentDirectory + "\\bin"))
                    Directory.Delete(Environment.CurrentDirectory + "\\bin", true);
                Directory.Move(extractPath + "\\ffmpeg-master-latest-win64-gpl-shared\\bin\\", Environment.CurrentDirectory + "\\bin");
                this.Dispatcher.Invoke(async () =>
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Update Finished",
                        Content = "FFmpeg has been updated!",
                        PrimaryButtonText = "Okayy ^w^",
                        IsPrimaryButtonEnabled = true
                    };
                    await dialog.ShowAsync();
                });
                Directory.Delete(extractPath, true);
                File.Delete(zipPath);
            }

            this.Dispatcher.Invoke(() =>
            {
                dragDropDialog.IsEnabled = true;
                closePopupUI();
            });

            ffmpegPath = Environment.CurrentDirectory + "\\bin";
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegPath, TemporaryFilesFolder = "/tmp" });
        }).Start();

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
        if (inputFile)
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
        cancelTask = false;
        string[] paths = inPath.Text.Split("|");
        string[] outpaths = outPath.Text.Split("|");
        bool emptyOutput = outPath.Text == "" || outPath.Text.IsWhiteSpace() ? true : false;
        new Thread(() =>
        {
            if (paths.Count() >= 1)
            {
                int index = 0;
                foreach (String str in paths)
                {
                    if (cancelTask) return;
                    while (ongoingTask) {
                        
                    }
                    Dispatcher.Invoke(() =>
                    {
                        exportVideo(str, emptyOutput ? Path.GetDirectoryName(str) + "\\" + Path.GetFileNameWithoutExtension(str) + "-out" + Path.GetExtension(str) : outpaths[index], index, paths.Count());
                    });

                    index++;
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    exportVideo(paths[0], emptyOutput ? Path.GetDirectoryName(paths[0]) + "\\" + Path.GetFileNameWithoutExtension(paths[0]) + "-out" + Path.GetExtension(paths[0]) : outpaths[0]);
                });
            }
        }).Start();
    }
    bool ongoingTask = false;
    bool cancelTask = false;
    private async void exportVideo(string path, string outPath, int index = 0, int max = 0)
    {
        ongoingTask = true;
        string input = path.Replace("\"", "");
        string output = outPath.Replace("\"", "");
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
        options.Append(max >= 1 ? "Input Files:\n" + inPath.Text.Replace("|", "\n") : "Input File: " + path);

        if (this.outPath.Text == "" || this.outPath.Text.IsWhiteSpace())
        {
            options.Append("\nOutput Files:\n");
            foreach (String filePath in inPath.Text.Split("|"))
            {
                options.Append(Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + "-out" + Path.GetExtension(filePath) + "\n");
            }
        }
        else
        {
            options.Append("\nOutput File: " + outPath);
        }

        ContentDialog confirmDialog = new ContentDialog()
        {
            Title = "Confirmation",
            Content = "Are you sure you want to do the operation with these options?\n\n" + options,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No"
        };
        try
        {
            if (index >= 1 || await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (File.Exists(outPath))
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "W- waitt!!",
                        Content = "The specified file already exists!\nDo you want to overwrite it? o~o?",
                        PrimaryButtonText = "Yaa ÒwÓ",
                        SecondaryButtonText = "Naa ^w^",
                        IsPrimaryButtonEnabled = true,
                        IsSecondaryButtonEnabled = true
                    };
                    if (await dialog.ShowAsync() == ContentDialogResult.Secondary)
                    {
                        return;
                    }
                }

                var probe = FFProbe.Analyse(input);
                if (bitrate <= 0)
                    bitrate = 100;
                showProgress();
                new Thread(async () =>
                {
                    try
                    {
                        switch (codecTypeID)
                        {
                            case 0:
                                if (accelType == 1)
                                {
                                    await FFMpegArguments.FromFileInput(input, true,
                                     options => options.WithHardwareAcceleration(HardwareAccelerationDevice.Auto))
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec("h264_nvenc")
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                }
                                else if (accelType == 2)
                                    await FFMpegArguments.FromFileInput(input, true,
                                     options => options.WithHardwareAcceleration(HardwareAccelerationDevice.Auto))
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec("h264_amf")
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                else if (accelType == 3)
                                    await FFMpegArguments.FromFileInput(input, true,
                                     options => options.WithHardwareAcceleration(HardwareAccelerationDevice.Auto))
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec("h264_qsv")
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                else
                                    await FFMpegArguments.FromFileInput(input, true)
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec(VideoCodec.LibX264)
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                break;
                            case 1:
                                if (accelType == 1)
                                {
                                    await FFMpegArguments.FromFileInput(input, true,
                                     options => options.WithHardwareAcceleration(HardwareAccelerationDevice.Auto))
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec("hevc_nvenc")
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                }
                                else if (accelType == 2)
                                    await FFMpegArguments.FromFileInput(input, true,
                                     options => options.WithHardwareAcceleration(HardwareAccelerationDevice.Auto))
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec("hevc_amf")
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                else if (accelType == 3)
                                    await FFMpegArguments.FromFileInput(input, true,
                                     options => options.WithHardwareAcceleration(HardwareAccelerationDevice.Auto))
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec("hevc_qsv")
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                else
                                    await FFMpegArguments.FromFileInput(input, true)
                                .OutputToFile(output, true, options => options
                                    .WithVideoCodec(VideoCodec.LibX265)
                                    .WithConstantRateFactor(bitrate)
                                    .WithAudioCodec(AudioCodec.Aac))
                                .NotifyOnProgress((percentage) =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        progress.Value = percentage;
                                        popupTitle.Content = "Conversion In Progress!";
                                        status.Content = "Please do not close the app.\nProgress: " + percentage + "%" + "\n(" + (index + 1) + " of " + max + ")";

                                        status.Dispatcher.Invoke(() =>
                                        {
                                            if (percentage == 100)
                                            {
                                                status.Content = "Finished!" + "\n(" + (index + 1) + " of " + max + ")";
                                                popupTitle.Content = "Conversion Finished!";
                                                progress.Value = 100;
                                                closePopup.IsEnabled = true;
                                                ongoingTask = false;
                                            }
                                        });

                                    });
                                }, probe.Duration)
                                .ProcessAsynchronously();
                                break;
                        }
                    }
                    catch (Exception err)
                    {
                        exception(err);
                    }
                }).Start();
            }
        }
        catch (Exception ex)
        {
            ongoingTask = false;
            cancelTask = true;
            exception(ex);
            return;
        }
    }

    private void exception(Exception e)
    {
        this.Dispatcher.Invoke(async () =>
        {
            string logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileName = "\\FFUI-" + DateTime.Now.Millisecond + ".txt";
            string name = logPath + fileName;
            try
            {
                File.WriteAllText(name, e.Message);
            }
            catch (Exception err)
            {
                MessageBox.Show("Unable to write logfile:\n" + err.Message, "FallbackDialog: Log write failed.");
                return;
            }

            ContentDialog dialog = new ContentDialog
            {
                Title = "Oh nooo!",
                Content = "The current task failed to execute.\nCheck the logs in your documents folder named " + fileName,
                PrimaryButtonText = "Okay qwq",
                IsPrimaryButtonEnabled = true
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                closePopupUI();
            }
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
        ta.Completed += (o, t) =>
        {
            popupOnProgress.Visibility = Visibility.Collapsed;
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
        //instance.bitrate = specifiedBitrate;
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

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        BlurEffect be = new BlurEffect();
        be.RenderingBias = RenderingBias.Performance;
        main.Effect = be;
        dragDropDialog.Opacity = 0;
        dragDropDialog.Visibility = Visibility.Visible;
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
        DoubleAnimation daOpacity1 = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        be.BeginAnimation(BlurEffect.RadiusProperty, da);
        main.BeginAnimation(Grid.OpacityProperty, daOpacity);
        dragDropDialog.BeginAnimation(Border.OpacityProperty, daOpacity1);
    }

    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        BlurEffect be = new BlurEffect();
        be.RenderingBias = RenderingBias.Performance;
        main.Effect = be;
        DoubleAnimation da = new DoubleAnimation
        {
            From = 20,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation daOpacity = new DoubleAnimation
        {
            From = 0.3,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation daOpacity1 = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        be.BeginAnimation(BlurEffect.RadiusProperty, da);
        main.BeginAnimation(Grid.OpacityProperty, daOpacity);
        dragDropDialog.BeginAnimation(Border.OpacityProperty, daOpacity1);
        dragDropDialog.Visibility = Visibility.Collapsed;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        // DragLeave Animation Copy Pasted.
        BlurEffect be = new BlurEffect();
        be.RenderingBias = RenderingBias.Performance;
        main.Effect = be;
        DoubleAnimation da = new DoubleAnimation
        {
            From = 20,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation daOpacity = new DoubleAnimation
        {
            From = 0.3,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DoubleAnimation daOpacity1 = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        be.BeginAnimation(BlurEffect.RadiusProperty, da);
        main.BeginAnimation(Grid.OpacityProperty, daOpacity);
        dragDropDialog.BeginAnimation(Border.OpacityProperty, daOpacity1);
        dragDropDialog.Visibility = Visibility.Collapsed;
        // Actual Drop Function
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.AllowDrop = false;
            main.AllowDrop = false;

            // Assuming you have one file that you care about, pass it off to whatever
            // handling code you have defined.
            if (files.Length > 1)
            {
                //ContentDialog dialog = new ContentDialog
                //{
                //    Title = "Oh nooo!",
                //    Content = "You can only drop ONE file.\nThis feature is planned for the next update.\nStay tuned!",
                //    PrimaryButtonText = "Awh okay :<",
                //    IsPrimaryButtonEnabled = true
                //};
                //if(await dialog.ShowAsync() == ContentDialogResult.Primary)
                //{
                //    this.AllowDrop = true;
                //    main.AllowDrop = true;
                //}

                string extension = System.IO.Path.GetExtension(files[0]).ToLowerInvariant();
                if (extension == ".mp4" || extension == ".mkv" || extension == ".webm" || extension == ".avi" ||
           extension == ".mov" || extension == ".flv" || extension == ".wmv" || extension == ".mpeg" ||
           extension == ".mpg" || extension == ".m4v" || extension == ".3gp" || extension == ".ts" ||
           extension == ".mts" || extension == ".m2ts" || extension == ".ogv" || extension == ".vob" ||
           extension == ".rm" || extension == ".rmvb" || extension == ".asf" || extension == ".f4v" ||
           extension == ".divx" || extension == ".dv" || extension == ".amv" || extension == ".nsv" ||
           extension == ".mjpeg" || extension == ".mjpg")
                {
                    inPath.Text = "";
                    foreach (String str in files)
                    {
                        inPath.Text += str + "|";
                    }
                    if (inPath.Text.EndsWith("|"))
                    {
                        inPath.Text = inPath.Text.Substring(0, inPath.Text.Length - 1);
                    }
                }
                else
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Oh nooo!",
                        Content = "Please only insert video files!\nYou make me sad QwQ.",
                        PrimaryButtonText = "Awh okay :<",
                        IsPrimaryButtonEnabled = true
                    };
                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        this.AllowDrop = true;
                        main.AllowDrop = true;
                    }
                    return;
                }
                this.AllowDrop = true;
                main.AllowDrop = true;
                return;
            }
            else
            {
                // Check if the file type is a video.
                string extension = System.IO.Path.GetExtension(files[0]).ToLowerInvariant();
                if (extension == ".mp4" || extension == ".mkv" || extension == ".webm" || extension == ".avi" ||
           extension == ".mov" || extension == ".flv" || extension == ".wmv" || extension == ".mpeg" ||
           extension == ".mpg" || extension == ".m4v" || extension == ".3gp" || extension == ".ts" ||
           extension == ".mts" || extension == ".m2ts" || extension == ".ogv" || extension == ".vob" ||
           extension == ".rm" || extension == ".rmvb" || extension == ".asf" || extension == ".f4v" ||
           extension == ".divx" || extension == ".dv" || extension == ".amv" || extension == ".nsv" ||
           extension == ".mjpeg" || extension == ".mjpg")
                {
                    inPath.Text = files[0].ToString();
                }
                else
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Oh nooo!",
                        Content = "Please only insert video files!\nYou make me sad QwQ.",
                        PrimaryButtonText = "Awh okay :<",
                        IsPrimaryButtonEnabled = true
                    };
                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        this.AllowDrop = true;
                        main.AllowDrop = true;
                    }
                    return;
                }
            }
            this.AllowDrop = true;
            main.AllowDrop = true;
        }

    }

    private async void updateFFmpeg_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "Update Confirmation",
            Content = "Are you sure you want to update the FFmpeg binary? o~o?",
            PrimaryButtonText = "Yaa ^w^",
            SecondaryButtonText = "Naa -w-",
            IsPrimaryButtonEnabled = true,
            IsSecondaryButtonEnabled = true
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            checkFFmpeg(true);
        }
    }

    private void micaToggle_Checked(object sender, RoutedEventArgs e)
    {
        key.SetValue("mica", 1, RegistryValueKind.DWord);
        WindowHelper.SetSystemBackdropType(this, BackdropType.Tabbed);
        WindowHelper.SetApplyBackground(this, false);
    }

    private void micaToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        key.SetValue("mica", 0, RegistryValueKind.DWord);
        WindowHelper.SetSystemBackdropType(this, BackdropType.Acrylic);
        WindowHelper.SetApplyBackground(this, true);
    }

    public void showProgress(int type = 0)
    {
        switch (type)
        {
            case 0:
                popupTitle.Content = "Conversion In Progress";
                status.Content = "Setting up the FFmpeg Instance...";
                progress.IsIndeterminate = false;
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
                break;
            case 1:
                popupTitle.Content = "Download necessary files";
                status.Content = "Downloading FFmpeg. Please wait\nThis will only need to be done once unless you press the update FFmpeg button.";
                closePopup.IsEnabled = false;
                progress.IsIndeterminate = true;

                BlurEffect be2 = new BlurEffect();
                be2.RenderingBias = RenderingBias.Performance;
                main.Effect = be2;
                popupOnProgress.Visibility = Visibility.Visible;
                ThicknessAnimation ta2 = new ThicknessAnimation
                {
                    From = new Thickness(0, this.Height, 0, 0),
                    To = new Thickness(0, 0, 0, 0),
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                DoubleAnimation da2 = new DoubleAnimation
                {
                    From = 0,
                    To = 20,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                DoubleAnimation daOpacity2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0.3,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                be2.BeginAnimation(BlurEffect.RadiusProperty, da2);
                main.BeginAnimation(Grid.OpacityProperty, daOpacity2);
                main.IsEnabled = false;
                popupOnProgress.BeginAnimation(Grid.MarginProperty, ta2);
                break;
        }
    }
}