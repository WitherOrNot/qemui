using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace qemui
{
    /// <summary>
    /// Interaction logic for CreateDrive.xaml
    /// </summary>
    ///

    public partial class CreateDrive : Window
    {
        List<String> formats = new List<string>() { "QCOW2 Disk Image (*.qcow2)|*.qcow2", "VMWare Disk Image (*.vmdk)|*.vmdk", "VirtualBox Disk Image (*.vdi)|*.vdi", "Hyper-V Disk Image (*.vhdx)|*.vhdx" };
        List<String> qemu_formats = new List<String>() { "qcow2", "vmdk", "vdi", "vhdx" };
        public CreateDrive()
        {
            InitializeComponent();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(Math.Round(SizeSlider.Value, 0) - SizeSlider.Value) < 0.1)
            {
                SizeSlider.Value = Math.Round(SizeSlider.Value, 0);
            }
        }

        private string SliderToQEMUSize(double value)
        {
            double rambytes = 128 * 1024 * 1024 * Math.Pow(2, (double)value);

            if (rambytes >= 1 * 1024 * 1024 * 1024)
            {
                string gb = Math.Round(rambytes / (1024 * 1024 * 1024), 0) + "G";
                return gb;
            }
            else
            {
                string mb = Math.Round(rambytes / (1024 * 1024), 0) + "M";
                return mb;
            }
        }

        private void Create(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = formats[FormatSelector.SelectedIndex];
            if (fileDialog.ShowDialog() == true)
            {
                Process p = new Process();
                p.StartInfo.FileName = Properties.Settings.Default.QEMUPath + "qemu-img.exe";
                p.StartInfo.Arguments = "create -f " + qemu_formats[FormatSelector.SelectedIndex] + " " + fileDialog.FileName + " " + SliderToQEMUSize(SizeSlider.Value);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                ((NewVM)this.Owner).Add_Drive(fileDialog.FileName);
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
