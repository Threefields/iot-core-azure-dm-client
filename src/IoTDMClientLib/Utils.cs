﻿/*
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
using System;
using System.Diagnostics;
using Windows.Foundation.Diagnostics;

namespace Microsoft.Devices.Management
{
    static class Constants
    {
        public const string IoTDMFolder = "C:\\Data\\Users\\DefaultAccount\\AppData\\Local\\Temp\\IotDm";
        public const string ETWGuid = "D198EE7D-C5F1-4F5F-95BE-A2EE6FA45897";   // Use this guid with xperf, the CSP, etc.
        public const string ETWChannelName = "AzureDM";
    }

    class Error : Exception
    {
        public Error() { }

        public Error(int code, string message) : base(message)
        {
            this.HResult = code;
        }
    }

    enum JsonReport
    {
        Report,
        Unreport
    }

    public class Logger
    {
        public static void Log(string message, LoggingLevel level)
        {
            /*
                You can collect the events generated by this method with xperf or another
                ETL controller tool. To collect these events in an ETL file:

                xperf -start MySession -f MyFile.etl -on Constants.ETWGuid
                (call LogError())
                xperf -stop MySession

                After collecting the ETL file, you can decode the trace using xperf, wpa,
                or tracerpt. For example, to decode MyFile.etl with tracerpt:

                tracerpt MyFile.etl
                (generates dumpfile.xml)
            */

            using (var channel = new LoggingChannel(Constants.ETWChannelName, null /*default options*/, new Guid(Constants.ETWGuid)))
            {
                Debug.WriteLine("[" + Constants.ETWChannelName + "]    [" + level.ToString() + "]    " + message);
                channel.LogMessage(message, level);
            }
        }

    }
}