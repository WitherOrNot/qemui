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
    /// Interaction logic for EditVM.xaml
    /// </summary>
    /// 

    public partial class EditVM : Window
    {
        public List<String> drives = new List<String>();
        public VM editVM;

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

        public EditVM(VM editVM)
        {
            AddKeyCommand(Key.O, ModifierKeys.Control, Import_Drive);
            AddKeyCommand(Key.N, ModifierKeys.Control, Create_Drive);
            AddKeyCommand(Key.Delete, Remove_Drive);

            this.editVM = editVM;
            
            foreach (string drive in editVM.drives)
            {
                drives.Add(drive);
            }

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EditLabel.Content += editVM.Name;
            VMName.Text = editVM.Name;
            DriveList.ContextMenu = (ContextMenu)this.FindResource("DriveMenu");

            if (editVM.cdrom != null)
            {
                FilePathBox.Text = editVM.cdrom;
            }

            RAMSlider.Value = QEMUSizeToSlider(editVM.ram);

            foreach (string drive in editVM.drives)
            {
                ListBoxItem item = new ListBoxItem
                {
                    Content = drive
                };
                DriveList.Items.Add(item);
            }

            HAX_Accel.IsChecked = editVM.hax_accel;
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
            if (Math.Abs(Math.Round(RAMSlider.Value, 0) - RAMSlider.Value) < 0.05)
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

        private double QEMUSizeToSlider(string size)
        {
            double value = 0;

            if (size.EndsWith("G"))
            {
                int gb = int.Parse(size.TrimEnd('G'));
                value = 3 + Math.Log(gb, 2);
            } else if (size.EndsWith("M"))
            {
                double mb = double.Parse(size.TrimEnd('M')) / 128;
                value = Math.Log(mb, 2);
            }

            return value;
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
            editVM.Name = VMName.Text;
            editVM.cdrom = FilePathBox.Text;
            editVM.ram = SliderToQEMUSize(RAMSlider.Value);
            editVM.hax_accel = (HAX_Accel.IsChecked == true);

            editVM.drives.Clear();
            foreach (string drive in drives)
            {
                editVM.drives.Add(drive);
            }

            ((MainWindow)this.Owner).EditVM(editVM.ID, editVM);
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
