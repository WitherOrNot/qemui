using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for NewVM.xaml
    /// </summary>
    /// 

    public class RAMSliderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double rambytes = 128 * 1024 * 1024 * Math.Pow(2, (double)value);

            if (rambytes >= 1 * 1024 * 1024 * 1024)
            {
                string gb = Math.Round(rambytes / (1024 * 1024 * 1024), 0) + " GB";
                return gb;
            } else
            {
                string mb = Math.Round(rambytes / (1024 * 1024), 0) + " MB";
                return mb;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }
    
    public partial class NewVM : Window
    {
        VM newVM;
        List<String> drives = new List<String>();

        private void AddKeyCommand(Key key, ModifierKeys modifier, ExecutedRoutedEventHandler handler)
        {
            RoutedCommand cmd = new RoutedCommand();
            cmd.InputGestures.Add(new KeyGesture(key, modifier));
            CommandBindings.Add(new CommandBinding(cmd, handler));
        }

        private void AddKeyCommand(Key key, ExecutedRoutedEventHandler handler)
        {
            RoutedCommand cmd = new RoutedCommand();
            cmd.InputGestures.Add(new KeyGesture(key));
            CommandBindings.Add(new CommandBinding(cmd, handler));
        }

        public NewVM()
        {
            AddKeyCommand(Key.O, ModifierKeys.Control, Import_Drive);
            AddKeyCommand(Key.N, ModifierKeys.Control, Create_Drive);
            AddKeyCommand(Key.Delete, Remove_Drive);

            InitializeComponent();
            this.newVM = new VM();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DriveList.ContextMenu = (ContextMenu)this.FindResource("DriveMenu");
        }

        private void Open_CDROM(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "ISO Disk Image (*.iso)|*.iso|All Files(*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                FilePathBox.Text = fileDialog.FileName;
            }
        }

        private void Import_Drive(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Drive Image File (*.qcow2, *.vmdk, *.vdi, *.vhdx)| *.qcow2;*.vmdk;*.vdi;*.vhdx";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                foreach (string file in fileDialog.FileNames)
                {
                    ListBoxItem drive = new ListBoxItem();
                    drive.Content = file;
                    DriveList.Items.Add(drive);
                    drives.Add(file);
                }
            }
        }

        public void Add_Drive(string drive)
        {
            ListBoxItem drive_item = new ListBoxItem
            {
                Content = drive
            };
            DriveList.Items.Add(drive_item);
            drives.Add(drive);
        }

        private void Remove_Drive(object sender, RoutedEventArgs e)
        {
            List<ListBoxItem> remove = new List<ListBoxItem>();
            foreach (ListBoxItem item in DriveList.Items)
            {
                remove.Add(item);
            }

            foreach (ListBoxItem item in remove)
            {
                DriveList.Items.Remove(item);
                drives.Remove((string)item.Content);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(Math.Round(RAMSlider.Value,0) - RAMSlider.Value) < 0.05)
            {
                RAMSlider.Value = Math.Round(RAMSlider.Value, 0);
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

        private void Create_Drive(object sender, RoutedEventArgs e)
        {
            CreateDrive createDrive = new CreateDrive
            {
                Owner = this
            };
            createDrive.ShowDialog();
        }

        private void Create(object sender, RoutedEventArgs e)
        {
            if (VMName.Text.Trim() == "")
            {
                MessageBox.Show("Name cannot be blank!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (((MainWindow)this.Owner).vmlist.vms.Count >= 1)
            {
                newVM.ID = ((MainWindow)this.Owner).vmlist.vms.Last().ID + 1;
            } else
            {
                newVM.ID = 0;
            }
                
            newVM.hax_accel = (HAX_Accel.IsChecked == true);
            newVM.Name = VMName.Text;
            newVM.cdrom = FilePathBox.Text;
            newVM.ram = SliderToQEMUSize(RAMSlider.Value);
            
            foreach (string drive in drives)
            {
                newVM.drives.Add(drive);
            }

            ((MainWindow)this.Owner).AddVM(newVM);
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Remove_CDROM(object sender, RoutedEventArgs e)
        {
            FilePathBox.Text = "";
        }
    }
}
