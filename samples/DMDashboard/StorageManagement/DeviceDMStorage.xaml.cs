using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DMDashboard.StorageManagement
{
    /// <summary>
    /// Interaction logic for DeviceDMStorage.xaml
    /// </summary>
    public partial class DeviceDMStorage : Window
    {
        public DeviceDMStorage(DeviceTwinAndMethod azureDevice)
        {
            InitializeComponent();

            _azureDevice = azureDevice;
        }

        private async Task EnumFolderAsync()
        {
            CancellationToken cancellationToken = new CancellationToken();
            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod("windows.enumDMFolders", "{}", new TimeSpan(0, 0, 30), cancellationToken);
            MessageBox.Show("enumDMFolders Result:\nStatus: " + result.Status + "\nPayload: " + result.Payload);

            FoldersList.Items.Clear();

            JObject jsonObject = (JObject)JsonConvert.DeserializeObject(result.Payload);
            JArray jsonArray = (JArray)jsonObject["list"];
            foreach(object o in jsonArray)
            {
                if (!(o is JValue))
                {
                    continue;
                }
                JValue v = (JValue)o;
                if (!(v.Value is string))
                {
                    continue;
                }
                FoldersList.Items.Add((string)v.Value);
            }
        }

        private void OnEnumFolders(object sender, RoutedEventArgs e)
        {
            EnumFolderAsync();
        }

        private async Task EnumFilesAsync()
        {
            if (-1 == FoldersList.SelectedIndex)
            {
                MessageBox.Show("Select a folder first.");
                return;
            }

            string folderName = (string)FoldersList.SelectedItem;

            CancellationToken cancellationToken = new CancellationToken();
            string paramsString = "{ \"folder\" : \"" + folderName + "\"}";
            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod("windows.enumDMFiles", paramsString, new TimeSpan(0, 0, 30), cancellationToken);
            MessageBox.Show("enumDMFiles Result:\nStatus: " + result.Status + "\nPayload: " + result.Payload);

            FilesList.Items.Clear();

            JObject jsonObject = (JObject)JsonConvert.DeserializeObject(result.Payload);
            JArray jsonArray = (JArray)jsonObject["list"];
            foreach (object o in jsonArray)
            {
                if (!(o is JValue))
                {
                    continue;
                }
                JValue v = (JValue)o;
                if (!(v.Value is string))
                {
                    continue;
                }
                FilesList.Items.Add((string)v.Value);
            }
        }

        private void OnEnumFiles(object sender, RoutedEventArgs e)
        {
            EnumFilesAsync();
        }

        private async Task DeleteAsync()
        {
            if (-1 == FoldersList.SelectedIndex || -1 == FilesList.SelectedIndex)
            {
                MessageBox.Show("Select a folder and a file first.");
                return;
            }

            string folderName = (string)FoldersList.SelectedItem;
            string fileName = (string)FilesList.SelectedItem;

            CancellationToken cancellationToken = new CancellationToken();
            string paramsString = "{ \"folder\" : \"" + folderName + "\", \"file\" : \"" + fileName + "\"}";
            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod("windows.deleteDMFile", paramsString, new TimeSpan(0, 0, 30), cancellationToken);
            MessageBox.Show("enumDMFiles Result:\nStatus: " + result.Status + "\nPayload: " + result.Payload);

            EnumFilesAsync();
        }

        private void OnRetrieve(object sender, RoutedEventArgs e)
        {
            if (-1 == FoldersList.SelectedIndex || -1 == FilesList.SelectedIndex)
            {
                MessageBox.Show("Select a folder and a file first.");
                return;
            }

            string folderName = (string)FoldersList.SelectedItem;
            string fileName = (string)FilesList.SelectedItem;

            DMFileRetrieval dmFileRetrieval = new DMFileRetrieval(_azureDevice, folderName, fileName);
            dmFileRetrieval.Owner = this;
            dmFileRetrieval.DataContext = null;
            dmFileRetrieval.ShowDialog();
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            DeleteAsync();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        DeviceTwinAndMethod _azureDevice;
    }
}
