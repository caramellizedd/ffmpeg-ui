using FFmpeg.NET;
using FFmpeg.NET.Enums;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ffmpeg_ui;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        checkExportConditions();
    }
    bool inputFile = false, outputFile = false;
    // Arguments
    bool amdAccel = false;
    bool nvidiaAccel = false;
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
        string options = "";
        if (options == "")
            options = "convert only";
        ContentDialog confirmDialog = new ContentDialog()
        {
            Title = "Confirmation",
            Content = "Are you sure you want to do the operation with these options?\n" + options,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No"
        };
        if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var tokenSource = new CancellationTokenSource();
            var inputFile = new InputFile(inPath.Text);
            var outputFile = new OutputFile(outPath.Text);
            Engine ffmpeg = new Engine(Directory.GetCurrentDirectory() + "\\ffmpeg.exe");
            var conversionOptions = new ConversionOptions
            {
                HWAccel = HWAccel.None
            };
            if(amdAccel)
                conversionOptions = new ConversionOptions
                {
                    HWAccel = HWAccel.d3d11va
                };
            else if(nvidiaAccel)
                conversionOptions = new ConversionOptions
                {
                    HWAccel = HWAccel.cuda
                };

            await ffmpeg.ConvertAsync(inputFile, outputFile, conversionOptions, tokenSource.Token);
        }
    }

    public void startExport()
    {

    }
}