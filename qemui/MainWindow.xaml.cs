﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
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
using System.Windows.Navigation;
using System.Xml.Serialization;

namespace qemui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    
    public partial class MainWindow : Window
    {
        string VMListPath = Properties.Settings.Default.VMListPath;
        public VMList vmlist;
        VM selectedVM;

        public MainWindow()
        {
            if (VMListPath == "" || !Directory.Exists(Path.GetDirectoryName(VMListPath)))
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string vmlist_folder = Path.Combine(appdata, "qemui");

                if (!Directory.Exists(vmlist_folder))
                {
                    Directory.CreateDirectory(vmlist_folder);
                }

                string vmlist = Path.Combine(vmlist_folder, "vmdata.xml");
                Properties.Settings.Default.VMListPath = vmlist;
                Properties.Settings.Default.Save();
                VMListPath = Properties.Settings.Default.VMListPath;
            }

            if (File.Exists(VMListPath))
            {
                FileStream fs = new FileStream(VMListPath, FileMode.Open);
                XmlSerializer serializer = new XmlSerializer(typeof(VMList));
                vmlist = (VMList)serializer.Deserialize(fs);
                fs.Close();
            } else
            {
                vmlist = new VMList();
            }

            if (!File.Exists(Properties.Settings.Default.QEMUPath + "qemu-system-x86_64.exe"))
            {
                bool success = false;
                while (MessageBox.Show("QEMU Binary not found. Please select the QEMU Binary for qemui to use.", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
                {
                    OpenFileDialog openQEMU = new OpenFileDialog();
                    openQEMU.Filter = "QEMU Binary|qemu-system-x86_64.exe";
                    openQEMU.InitialDirectory = "C:\\Program Files\\";
                    if (openQEMU.ShowDialog() == true)
                    {
                        Properties.Settings.Default.QEMUPath = Path.GetDirectoryName(openQEMU.FileName) + "\\";
                        Properties.Settings.Default.Save();
                        success = true;
                        break;
                    }
                }

                if (!success)
                    this.Close();
            }

            InitializeComponent();
        }

        private void UpdateListView()
        {
            int selected = VMListView.SelectedIndex;
            VMListView.Items.Clear();
            foreach (VM vm in vmlist.vms)
            {
                ListBoxItem item = new ListBoxItem
                {
                    Content = vm.Name,
                    Name = "VM_" + vm.ID
                };
                VMListView.Items.Add(item);
            }
            VMListView.SelectedIndex = selected;
            VMListView_Selected(VMListView, null);
        }

        public void AddVM(VM vm)
        {
            vmlist.Add(vm);
            UpdateListView();
            vmlist.Save();
        }

        public void EditVM(int vmid, VM editVM)
        {
            for (int i = 0; i < vmlist.vms.Count; i++)
            {
                if (vmlist.vms[i].ID == vmid)
                {
                    vmlist.vms[i] = editVM;
                }
            }
        }

        private void NewVM_Click(object sender, RoutedEventArgs e)
        {
            NewVM newVM = new NewVM
            {
                Owner = this
            };
            newVM.ShowDialog();
            UpdateListView();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Control control in VMData.Children)
            {
                control.Visibility = Visibility.Hidden;
            }
            UpdateListView();
        }

        private void VMListView_Selected(object sender, RoutedEventArgs e)
        {
            if (VMListView.SelectedIndex >= 0)
            {
                selectedVM = vmlist.vms.ElementAt(VMListView.SelectedIndex);
                foreach (Control control in VMData.Children)
                {
                    control.Visibility = Visibility.Visible;
                }
                LaunchArgsBox.Text = selectedVM.toArgs();

                VMDrives.Items.Clear();
                foreach (string drive in selectedVM.drives)
                {
                    ListBoxItem item = new ListBoxItem
                    {
                        Content = drive
                    };
                    VMDrives.Items.Add(item);
                }
            } else
            {
                foreach (Control control in VMData.Children)
                {
                    control.Visibility = Visibility.Hidden;
                }
            }
        }

        private void StartVM(object sender, RoutedEventArgs e)
        {
            if (!selectedVM.isRunning)
            {
                ((ListBoxItem)VMListView.SelectedItem).Content += " - Running";
                selectedVM.Start();
            }
        }

        public void VM_Killed(VM vm)
        {
            foreach (ListBoxItem item in VMListView.Items)
            {
                if (item.Name == "VM_" + vm.ID)
                {
                    item.Content = vm.Name;
                }
            }
        }

        private void StopVM(object sender, RoutedEventArgs e)
        {
            if (selectedVM.isRunning)
            {
                ((ListBoxItem)VMListView.SelectedItem).Content = selectedVM.Name;
                selectedVM.Stop();
            }
        }

        private void Delete(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Are you sure you want to delete " + selectedVM.Name + "?", "Delete VM", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                res = MessageBox.Show("Would you like to delete the drive images as well?", "Delete Drive Images", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    foreach (string drive in selectedVM.drives)
                    {
                        try
                        {
                            File.Delete(drive);
                        } catch
                        {

                        }
                    }
                }

                vmlist.vms.Remove(selectedVM);
                UpdateListView();
                vmlist.Save();
            }
        }

        private void EditVM_Click(object sender, RoutedEventArgs e)
        {
            EditVM editVM = new EditVM(selectedVM);
            editVM.Owner = this;
            if (editVM.ShowDialog() == true)
            {
                UpdateListView();
                vmlist.Save();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool warn = false;
            foreach (VM vm in vmlist.vms)
            {
                if (vm.isRunning)
                {
                    warn = true;
                }
            }

            if (warn)
            {
                if (MessageBox.Show("You are about to stop all active VMs. Would you like to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    foreach (VM vm in vmlist.vms)
                    {
                        vm.Stop();
                    }
                } else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
