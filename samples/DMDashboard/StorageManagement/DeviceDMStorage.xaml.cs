/*
Copyright 2017 Microsoft
Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
and associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DMDashboard.StorageManagement
{
    public partial class DeviceDMStorage : Window
    {
        const string EnumDMFoldersMethod = "windows.enumDMFolders";
        const string EnumDMFilesMethod = "windows.enumDMFiles";
        const string DeleteDMFileMethod = "windows.deleteDMFile";
        const string PropList = "list";
        const string PropFolder = "folder";
        const string PropFile = "file";

        public DeviceDMStorage(DeviceTwinAndMethod azureDevice)
        {
            InitializeComponent();

            _azureDevice = azureDevice;
        }

        private async Task EnumFolderAsync()
        {
            CancellationToken cancellationToken = new CancellationToken();
            string paramsString = "{}";
            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod(EnumDMFoldersMethod, paramsString, new TimeSpan(0, 0, 30), cancellationToken);

            FoldersList.Items.Clear();

            JObject jsonObject = (JObject)JsonConvert.DeserializeObject(result.Payload);
            string [] folderNames = Utils.GetStringArray(jsonObject, PropList);
            foreach (string folderName in folderNames)
            {
                FoldersList.Items.Add(folderName);
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
            StringBuilder parameters = new StringBuilder();
            parameters.Append("{\n");
            parameters.Append("  \"" + PropFolder + "\" : \"" + folderName + "\"\n");
            parameters.Append("}");

            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod(EnumDMFilesMethod, parameters.ToString(), new TimeSpan(0, 0, 30), cancellationToken);

            FilesList.Items.Clear();

            JObject jsonObject = (JObject)JsonConvert.DeserializeObject(result.Payload);
            string[] fileNames = Utils.GetStringArray(jsonObject, PropList);
            foreach (string fileName in fileNames)
            {
                FilesList.Items.Add(fileName);
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
            StringBuilder parameters = new StringBuilder();
            parameters.Append("{\n");
            parameters.Append("  \"" + PropFolder + "\" : \"" + folderName + "\",\n");
            parameters.Append("  \"" + PropFile + "\" : \"" + fileName + "\"\n");
            parameters.Append("}");
            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod(DeleteDMFileMethod, parameters.ToString(), new TimeSpan(0, 0, 30), cancellationToken);

            EnumFilesAsync();
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            DeleteAsync();
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
            dmFileRetrieval.ShowDialog();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        DeviceTwinAndMethod _azureDevice;
    }
}
