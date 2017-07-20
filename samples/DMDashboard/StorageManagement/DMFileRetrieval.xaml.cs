using System.Text;
using System.Threading;
using Microsoft.WindowsAzure.Storage;       // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob;  // Namespace for Blob storage types
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
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
    public partial class DMFileRetrieval : Window
    {
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

            StringBuilder paramsString = new StringBuilder();
            paramsString.Append("{\n");
            paramsString.Append("    \"folder\": \"" + _deviceFolder + "\",");
            paramsString.Append("    \"file\": \"" + _deviceFile + "\",");
            paramsString.Append("    \"connectionString\": \"" + connectionString + "\",");
            paramsString.Append("    \"container\": \"" + containerName + "\"");
            paramsString.Append("}\n");

            CancellationToken cancellationToken = new CancellationToken();
            DeviceMethodReturnValue result = await _azureDevice.CallDeviceMethod("windows.uploadDMFile", paramsString.ToString(), new TimeSpan(0, 0, 30), cancellationToken);
            MessageBox.Show("GoAsync Result:\nStatus: " + result.Status + "\nPayload: " + result.Payload);

            // ToDo: download to localFolder
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
