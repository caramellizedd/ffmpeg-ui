using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ffmpeg_ui
{
    /// <summary>
    /// Interaction logic for CustomizeArguments.xaml
    /// </summary>
    public partial class CustomizeArguments : ContentDialog
    {
        public CustomizeArguments()
        {
            InitializeComponent();
        }
        private static readonly Regex _regex = new Regex("[^0-9.]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            int accel = 0;
            int codec = 0;
            if ((bool)amdAccel.IsChecked)
            {
                accel = int.Parse((string)amdAccel.Tag);
            }
            else if ((bool)nvidiaAccel.IsChecked)
            {
                accel = int.Parse((string)nvidiaAccel.Tag);
            }
            else if ((bool)noAccel.IsChecked)
            {
                accel = int.Parse((string)noAccel.Tag);
            }
            if ((bool)h264.IsChecked)
            {
                codec = int.Parse((string)h264.Tag);
            }
            else if ((bool)hevc.IsChecked)
            {
                codec = int.Parse((string)hevc.Tag);
            }
            MainWindow.changeSettings(accel, codec, float.Parse(bitrate.Text));
        }

        private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
