using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace qemui
{
    [XmlRoot("VMList")]
    public class VMList
    {
        [XmlArrayItem("VM")]
        public List<VM> vms = new List<VM>();

        public void Add(VM vm)
        {
            vms.Add(vm);
        }

        public void Save()
        {
            FileStream fs = new FileStream(Properties.Settings.Default.VMListPath, FileMode.Create);
            XmlSerializer serializer = new XmlSerializer(typeof(VMList));
            serializer.Serialize(fs, this);
            fs.Close();
        }
    }

    public class VM
    {
        public int ID;

        public string Name;

        public bool hax_accel;

        [XmlElement("Drive")]
        public List<String> drives;

        public string cdrom;

        public string ram;

        public bool isRunning;

        private Process QEMUProcess;

        public VM()
        {
            QEMUProcess = new Process();
            drives = new List<String>();
        }

        public string toArgs()
        {
            string args = "";

            args += "-display sdl ";

            if (hax_accel)
                args += "-accel hax ";

            args += "-m " + ram + " ";

            if (cdrom != null)
                args += "-cdrom \"" + cdrom + "\" ";

            foreach (string drive in drives)
            {
                args += "-drive file=\"" + drive + "\",media=disk ";
            }

            return args;
        }

        public void Start()
        {
            if (!isRunning)
            {
                isRunning = true;
                QEMUProcess.StartInfo.FileName = Properties.Settings.Default.QEMUPath + "qemu-system-x86_64.exe";
                QEMUProcess.StartInfo.Arguments = this.toArgs();
                QEMUProcess.StartInfo.UseShellExecute = false;
                QEMUProcess.StartInfo.CreateNoWindow = true;
                QEMUProcess.EnableRaisingEvents = true;
                QEMUProcess.Start();
                QEMUProcess.Exited += new EventHandler(QEMUProcess_Exited);
            }
        }

        public void Start(string custom_args)
        {
            if (!isRunning)
            {
                isRunning = true;
                QEMUProcess.StartInfo.FileName = Properties.Settings.Default.QEMUPath + "qemu-system-x86_64.exe";
                QEMUProcess.StartInfo.Arguments = custom_args;
                QEMUProcess.StartInfo.UseShellExecute = false;
                QEMUProcess.StartInfo.CreateNoWindow = true;
                QEMUProcess.EnableRaisingEvents = true;
                QEMUProcess.Start();
                QEMUProcess.Exited += new EventHandler(QEMUProcess_Exited);
            }
        }

        private void QEMUProcess_Exited(object sender, EventArgs e)
        {
            isRunning = false;
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(
                    () => {
                        ((MainWindow)Application.Current.MainWindow).VM_Killed(this);
                    }
                )
            );
        }

        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;
                QEMUProcess.Kill();
            }
        }
    }
}
