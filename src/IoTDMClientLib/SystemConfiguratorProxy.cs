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
// #define DEBUG_COMMPROXY_OUTPUT

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Devices.Management.Message;
using Windows.Storage.Streams;
using Windows.System;

namespace Microsoft.Devices.Management
{

    // This class send requests (DMrequest) to the System Configurator and receives the responses (DMesponse) from it
    class SystemConfiguratorProxy : ISystemConfiguratorProxy
    {
        public async Task<IResponse> SendCommandAsync(IRequest command)
        {
            var processLauncherOptions = new ProcessLauncherOptions();
            var standardInput = new InMemoryRandomAccessStream();
            var standardOutput = new InMemoryRandomAccessStream();

            processLauncherOptions.StandardOutput = standardOutput;
            processLauncherOptions.StandardError = null;
            processLauncherOptions.StandardInput = standardInput.GetInputStreamAt(0);

            await command.Serialize().WriteToIOutputStreamAsync(standardInput);

            standardInput.Dispose();

            var processLauncherResult = await ProcessLauncher.RunToCompletionAsync(@"C:\Windows\System32\CommProxy.exe", "", processLauncherOptions);
            if (processLauncherResult.ExitCode == 0)
            {
                using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                {
                    var response = (await Blob.ReadFromIInputStreamAsync(outStreamRedirect)).MakeIResponse();
                    if (response.Status != ResponseStatus.Success)
                    {
                        var stringResponse = response as StringResponse;
                        string message = "Error: CommProxy.exe - Operation failed";
                        if (stringResponse != null)
                        {
                            message = "Error: " + stringResponse.Tag.ToString() + " : " + stringResponse.Response;
                        }
                        Debug.WriteLine(message);
                        throw new Exception(message);
                    }
                    return response;
                }
            }
            else
            {
                throw new Exception("CommProxy cannot read data from the input pipe");
            }
        }

        public Task<IResponse> SendCommand(IRequest command)
        {
            var processLauncherOptions = new ProcessLauncherOptions();
            var standardInput = new InMemoryRandomAccessStream();
            var standardOutput = new InMemoryRandomAccessStream();

            processLauncherOptions.StandardOutput = standardOutput;
            processLauncherOptions.StandardError = null;
            processLauncherOptions.StandardInput = standardInput.GetInputStreamAt(0);

            Windows.Foundation.IAsyncAction writeAsyncAction = command.Serialize().WriteToIOutputStreamAsync(standardInput);
            while (writeAsyncAction.Status == Windows.Foundation.AsyncStatus.Started)
            {
                Debug.WriteLine("Waiting to finish writing to output stream...");
                System.Threading.Tasks.Task.Delay(200);
            }
            standardInput.Dispose();

            Windows.Foundation.IAsyncOperation<ProcessLauncherResult> runAsyncAction = ProcessLauncher.RunToCompletionAsync(@"C:\Windows\System32\CommProxy.exe", "", processLauncherOptions);
            while (runAsyncAction.Status == Windows.Foundation.AsyncStatus.Started)
            {
                Debug.WriteLine("Waiting for CommProxy.exe to finish...");
                System.Threading.Tasks.Task.Delay(200);
            }
            ProcessLauncherResult processLauncherResult = runAsyncAction.GetResults();
            if (processLauncherResult.ExitCode == 0)
            {
                using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                {
                    Windows.Foundation.IAsyncOperation<Blob> readAsyncAction = Blob.ReadFromIInputStreamAsync(outStreamRedirect);
                    while (readAsyncAction.Status == Windows.Foundation.AsyncStatus.Started)
                    {
                        Debug.WriteLine("Waiting to finish reading from input stream...");
                        System.Threading.Tasks.Task.Delay(200);
                    }

                    var response = readAsyncAction.GetResults().MakeIResponse();
                    if (response.Status != ResponseStatus.Success)
                    {
                        var stringResponse = response as StringResponse;
                        string message = "Error: CommProxy.exe - Operation failed";
                        if (stringResponse != null)
                        {
                            message = "Error: " + stringResponse.Tag.ToString() + " : " + stringResponse.Response;
                        }
                        Debug.WriteLine(message);
                        throw new Exception(message);
                    }
                    return Task.FromResult<IResponse>(response);
                }
            }
            else
            {
                throw new Exception("CommProxy cannot read data from the input pipe");
            }
        }
    }
}
