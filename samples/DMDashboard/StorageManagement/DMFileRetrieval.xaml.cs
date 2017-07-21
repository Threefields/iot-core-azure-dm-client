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

using System.Text;
using System.Threading;
using Microsoft.WindowsAzure.Storage;       // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob;  // Namespace for Blob storage types
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace DMDashboard.StorageManagement
{
    public partial class DMFileRetrieval : Window
    {
        const string UploadDMFileMethod = "windows.uploadDMFile";
        const string PropFolder = "folder";
        const string PropFile = "file";
        const string PropConnectionString = "connectionString";
        const string PropContainerName = "container";

        public DMFileRetrieval(DeviceTwinAndMethod azureDevice, string deviceFolder, string deviceFile)
        {
            InitializeComponent();

            _azureDevice = azureDevice;
            _deviceFolder = deviceFolder;
            _deviceFile = deviceFile;

            FileName.Text = deviceFolder + "\\" + deviceFile;
        }

        private void OnEnumContainers(object sender, RoutedEventArgs e)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(AzureStorageConnectionString.Text);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            ContainersList.Items.Clear();
            foreach (var container in blobClient.ListContainers("", ContainerListingDetails.None, null, null))
            {
                ContainersList.Items.Add(container.Name);
            }
        }

        private async Task DownloadAsync(string connectionString, string containerName, string blobName, string localFolder)
        {
            uint maxTries = 10;
            uint wait = 2;  // in seconds
            string localFile = localFolder + "\\" + blobName;
            for (uint i = 0; i < maxTries; ++i)
            {
                try
                {
                    // Retrieve storage account from connection string.
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

                    // Create the blob client.
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    // Retrieve a reference to a container.
                    CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                    // ToDo: can we avoid throwing an exception?

                    // Retrieve reference to a named blob.
                    var blockBlob = container.GetBlockBlobReference(blobName);

                    // Save blob contents to a file.
                    await blockBlob.DownloadToFileAsync(localFile, System.IO.FileMode.CreateNew);

                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error downloading the file! " + e.Message);
                    // The file might not be there yet... try again in a bit...
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(1000 * wait));
                }
            }
        }

        private async Task GoAsync()
        {
            if (String.IsNullOrEmpty(AzureStorageConnectionString.Text))
            {
                MessageBox.Show("Need to specify the Azure Storage connection string!");
                return;
            }

            if (ContainersList.SelectedIndex == -1)
            {
                MessageBox.Show("Need to select a target container first!");
                return;
            }

            string connectionString = AzureStorageConnectionString.Text;
            string containerName = (string)ContainersList.SelectedItem;
            string localFolder = LocalFolder.Text;

            CancellationToken cancellationToken = new CancellationToken();
            StringBuilder parameters = new StringBuilder();
            parameters.Append("{\n");
            parameters.Append("    \"" + PropFolder + "\": \"" + _deviceFolder + "\",");
            parameters.Append("    \"" + PropFile + "\": \"" + _deviceFile + "\",");
            parameters.Append("    \"" + PropConnectionString + "\": \"" + connectionString + "\",");
            parameters.Append("    \"" + PropContainerName + "\": \"" + containerName + "\"");
            parameters.Append("}\n");
            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod(UploadDMFileMethod, parameters.ToString(), new TimeSpan(0, 0, 30), cancellationToken);

            System.IO.Directory.CreateDirectory(localFolder);
            DownloadAsync(connectionString, containerName, _deviceFile, localFolder);
        }

        private void OnGo(object sender, RoutedEventArgs e)
        {
            GoAsync();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        DeviceTwinAndMethod _azureDevice;
        string _deviceFolder;
        string _deviceFile;
    }
}
