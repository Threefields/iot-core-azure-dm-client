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

using Microsoft.Devices.Management.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Microsoft.Devices.Management
{
    class StorageHandler : IClientDirectMethodHandler
    {

        public StorageHandler(IClientHandlerCallBack callback, ISystemConfiguratorProxy systemConfiguratorProxy)
        {
            this._systemConfiguratorProxy = systemConfiguratorProxy;
            this._callback = callback;
        }

        // IClientDirectMethodHandler
        public IReadOnlyDictionary<string, Func<string, Task<string>>> GetDirectMethodHandler()
        {
            return new Dictionary<string, Func<string, Task<string>>>()
                {
                    { "windows.enumDMFolders" , EnumDMFolders },
                    { "windows.enumDMFiles" , EnumDMFiles },
                    { "windows.deleteDMFile" , DeleteDMFile },
                    { "windows.uploadDMFile" , UploadDMFile },
                };
        }

        private Task<string> EnumDMFolders(string jsonParam)
        {
            Debug.WriteLine("EnumDMFolders");
            var request = new Message.GetDMFoldersRequest();
            Task<IResponse> response = _systemConfiguratorProxy.SendCommand(request);
            StringListResponse listResponse = response.Result as StringListResponse;
            StringBuilder arrayString = new StringBuilder();

            foreach (string s in listResponse.List)
            {
                Debug.WriteLine("Found: " + s);
                if (arrayString.Length > 0)
                {
                    arrayString.Append(",\n");
                }
                string escaped = s.Replace("\\", "\\\\");
                arrayString.Append("\"" + escaped + "\"");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("    \"list\": [\n");
            if (arrayString.Length > 0)
            {
                sb.Append(arrayString.ToString());
                sb.Append("\n");
            }
            sb.Append("    ]\n");
            sb.Append("}\n");

            Debug.WriteLine("DM Folders:" + sb.ToString());
            return Task.FromResult(sb.ToString());
        }

        private Task<string> EnumDMFiles(string jsonParam)
        {
            Debug.WriteLine("EnumDMFiles");
            JObject o = (JObject)JsonConvert.DeserializeObject(jsonParam);
            string folderName = (string)o["folder"];
            Debug.WriteLine("folder name = " + folderName);
            var request = new Message.GetDMFilesRequest();
            request.DMFolderName = folderName;
            Task<IResponse> response = _systemConfiguratorProxy.SendCommand(request);
            StringListResponse listResponse = response.Result as StringListResponse;
            StringBuilder arrayString = new StringBuilder();

            foreach (string s in listResponse.List)
            {
                Debug.WriteLine("Found: " + s);
                if (arrayString.Length > 0)
                {
                    arrayString.Append(",\n");
                }
                string escaped = s.Replace("\\", "\\\\");
                arrayString.Append("\"" + escaped + "\"");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("    \"list\": [\n");
            if (arrayString.Length > 0)
            {
                sb.Append(arrayString.ToString());
                sb.Append("\n");
            }
            sb.Append("    ]\n");
            sb.Append("}\n");

            Debug.WriteLine("DM Folders:" + sb.ToString());
            return Task.FromResult(sb.ToString());
        }

        private Task<string> DeleteDMFile(string jsonParam)
        {
            Debug.WriteLine("DeleteDMFile");
            JObject o = (JObject)JsonConvert.DeserializeObject(jsonParam);

            string folderName = (string)o["folder"];
            string fileName = (string)o["file"];

            Debug.WriteLine("folder name = " + folderName);
            Debug.WriteLine("file name = " + fileName);

            var request = new Message.DeleteDMFileRequest();
            request.DMFolderName = folderName;
            request.DMFileName = fileName;
            Task<IResponse> response = _systemConfiguratorProxy.SendCommand(request);

            StringResponse listResponse = response.Result as StringResponse;
            if (listResponse.Status == ResponseStatus.Failure)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { response = "failed" }));
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { response = "succeeded" }));
        }

        private async Task UploadDMFileAsync(string jsonParam)
        {
            Debug.WriteLine("UploadDMFile");
            JObject o = (JObject)JsonConvert.DeserializeObject(jsonParam);

            // source
            string folderName = (string)o["folder"];
            string fileName = (string)o["file"];
            // azure storage
            string connectionString = (string)o["connectionString"];
            string containerName = (string)o["container"];

            var info = new Message.AzureFileTransferInfo();
            info.ConnectionString = connectionString;
            info.ContainerName = containerName;
            info.BlobName = fileName;
            info.Upload = true;
            info.LocalPath = Utils.IoTDMFolder + "\\" + folderName + "\\" + fileName;
            info.AppLocalDataPath = ApplicationData.Current.TemporaryFolder.Path + "\\" + fileName;

            AzureFileTransferRequest request = new AzureFileTransferRequest(info);
            var response = _systemConfiguratorProxy.SendCommand(request);
            if (response.Result.Status == ResponseStatus.Success)
            {
                Debug.WriteLine("Copy Succeeded!");
                var appLocalDataFile = await ApplicationData.Current.TemporaryFolder.GetFileAsync(fileName);
                Debug.WriteLine("Handle retrieved!");
                await IoTDMClient.AzureBlobFileTransfer.UploadFile(info, appLocalDataFile);
                Debug.WriteLine("File uploaded!");
                await appLocalDataFile.DeleteAsync();
                Debug.WriteLine("Local temporary file deleted!");
            }
        }

        private Task<string> UploadDMFile(string jsonParam)
        {
            UploadDMFileAsync(jsonParam);

            return Task.FromResult(JsonConvert.SerializeObject(new { response = "succeeded" }));
        }

        private ISystemConfiguratorProxy _systemConfiguratorProxy;
        private IClientHandlerCallBack _callback;
    }
}
