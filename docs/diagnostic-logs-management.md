# Diagnostic Logs Management

## Overview

Event Tracing for Windows (ETW) is a mechanism provided on the Windows platform that allows processes to log message with minimal overhead.

Part of how ETW achieves that is by moving all the logic of managing which messages go into log files, how the log files are named, their sizes, etc out of the running process.

This decoupling is done by the introduction of the concept of a 'provider' (i.e. the process that is writing the log message) and a 'collector' (i.e. the process that is reading the log messages).

The OS provides the infrastructure to connect both the providers with the collectors. The relationship between collectors and providers are many-to-many.

Both providers and collectors are identified on the system using ids upon their creation. Using those ids, the user can create a collector to listen to specific providers.

Collectors have properties that describe various things; for example: how the messages should be saved to a file, what is the max size of the file, etc. For each provider, the collectors also defined some properties, like which messages from that provider the collector should log (i.e. critical, error, information, etc).

The logical hierarchy is as follows:

<pre>
- Collector1
  - LogFileSizeLimitMB = 4
  - LogFileFolder = "c:\users\defaultaccount\logs"
  - Provider1
    - TraceLevel = critical
  - Provider2
    - TraceLevel = error
- Collector2
  - ...
</pre>

Above, `Collector1` is defined to listen to two providers; `Provider1` and `Provider2`. `Collector1` will write only critical messages from `Provider1` and error (or higher prioertiy) messages from `Provider2`.

## When to Use Azure DM Diagnostic Logs Management

A typical usage scenario is that there is a problem with a certain process running on the system.

- If that process does not log using ETW, then, this mechanism is not for it.
- If that process is using ETW, then it is a provider and it logs its message to the OS under a certain guid. The user needs to find out which guid it is using. Let's say it's <i>providerGuid</i>.

Once we have the provider(s) guid(s), we can define a collector and list the providers underneath - along with all the necessary configurations for the collector and the providers.

## How to Use Azure DM Diagnostic Logs Management

Here are the steps to capture logs to a file on disk:

- Identify the provider(s) you want to capture (find out the guids you need to collect).
- Create a collector, configure it, and add the providers you want captured to it.
- Start collection.
- Stop collection. This saves a log file on disk in the pre-configured folder.

Here are the steps to upload a log file:

- Enumerate all the files in the specified log folders.
- Provide the source file name on disk, the target Azure Storage parameters (connection string, container), and the Azure DM client will uplaod it for you.

## Creating, Configuring, and Starting/Stopping Collectors

Collectors are created by simply defining them in the device twin <i>desired</i> properties section. Each collector exposes a set of properties that its operation along with a set of providers and how each of them is processed by the collector.

Below is what the schema looks like:

### Collectors List and DeviceTwin Interaction

<pre>
"Windows_1" : {
    "eventTracingCollectors_2" : {
        "collector00_3" : {
            "reportProperties" : "yes"|"no",
            "applyProperties_4" : {collector configuration object}|"no"
            },
        "collector01_3" : {
            "reportProperties" : "yes"|"no",
            "applyProperties_4" : {collector configuration object}|"no"
            },
        "?" : "onlyCollectorNames" | "detailed" | "none"
        }
    }
}
</pre>

- `"eventTracingCollectors"`: is the node where all collector configurations appear under.
    - `"collector00"`: is the name of the collector being created.
        - `"reportProperties"`: indicates whether the state of this collector should be reported to the device twin or not. Allowed values are `"yes"` or `"no"`.
        - `"applyProperties"`: indicates which properties are to apply on the device - if any. Allowed values are:
            - `"no"`: indicates no properties to apply from the device twin. This makes sense only if the collector is already defined on the device and the user does not want to modify it.
            - `{collector configuration object}`: defines the collector configurations to apply. See below for more details.
    - `"?"`: specifies what that the DM client should report about the existing collectors. This can be used to enumerate defined collectors.
      - `"onlyCollectorNames"`: report only the names of the defined collectors.
      - `"detailed"`: report all details.
      - `"none"`: don't report except those that are marked to be reported in the desired section.

### Collector Configuration Object

<pre>
{
    "traceLogFileMode" : "sequential"|"circular",
    "logFileSizeLimitMB" : "<i>limit</i>",
    "logFileFolder" : "<i>collectorFolderName</i>",
    "start" : "yes" | "no",
    "guid00_5" : {provider configuration object},
    "guid01_5" : {provider configuration object}
}
</pre>

- `"traceLogFileMode"`: specifies the log file logging mode. Allowed values are `"sequential"` or `"circular"`.
- `"logFileSizeLimitMB"`: specifies the limit for the log file in megabytes. The default value is 4, and the acceptable range is 1-2048.
- `"collectorFolderName"`: specifies the relative path to the user's data folder where the log files of that collector will be saved once collection stops. The files can later be enumerated and uploaded to Azure Storage. See below for more details.
- `"start"`: specifies whether the collector should be active (i.e. collecting) or not. Its value is applied everytime the DM client service starts, or the property changes.
  - If this is set to `"yes"`, the collector will be started (if it is not already).
  - If this is set to `"no"`, the collector will be stopped, and a file will be saved in <i>logFileFolder</i> (if it is already running).
- `"guid00"`: specifies the <i>provider configuration object</i> for this guid. See below for more details.

### Provider Configuration Object

<pre>
{
    "traceLevel": "critical"|"error"|"warning"|"information"|"verbose",
    "keywords": "<i>see below</i>",
    "enabled": true|false,
    "type": "provider"
}
</pre>

- `"traceLevel"`: specifies the level of detail included in the trace log. Allowed values are `"critical"`, `"error"`, `"warning"`, `"information"`, and `"verbose"`.
- `"keywords"`: specifies the provider keywords to be used as MatchAnyKeyword for this provider.
- `"enabled": specifies if this provider is enabled in the trace session. Allowed values are `true` or `false`.
- `"type"`: specifies that this object is a provider object for the DM client. The only allowed value is `provider`.

## Reporting

Collectors are reported if:

- They are defined in the desired section and their `"reportProperties"` is set to `"yes"`. For those, all details are always reported.
- The `"?"` is specified in the collectors list in the desired properties section. The level of details will depend on its value - `"detailed"` or `"onlyCollectorNames"`.

The reporting for `"detailed"` looks like this:

<pre>
"windows_1" : {
    "eventTracingLogs_2" : {
        "collectorName00_3" : {
            "traceStatus" : "stopped"|"started",
            "traceLogFileMode" : "sequentual"|"circular"",
            "logFileSizeLimitMB" : "4",
            "logFileFolder" : "collectorFolderName",
            "guid00_4" : {
                "traceLevel" : "",
                "keywords" : "",
                "enabled" : true|false,
                "type" : "provider"
            }
        }
    }
}
</pre>

The reporting for `"onlyCollectorNames"` looks like this:
<pre>
"windows_1" : {
    "eventTracingLogs_2" : {
        "collectorName00_3" : "",
        "collectorName01_3" : "",
        "collectorName02_3" : ""
        }
    }
}
</pre>

## Working with Log Files

To upload collected log files, 

- The device must be online.
- Invoke the direct method `windows.enumerateETWLogs` to get the list of saved log files.
- Invoke the direct method `windows.uploadFile` to upload a specific file from disk to Azure Storage.

To delete collected log files from the device,

- Invoke the direct method `windows.deleteFile`.

### windows.enumerateETWLogs

Call this method to get a list of the saved log files under the log specified log folder.

#### Input

<pre>
{
    "folder" : "<i>relativeFolderName</i>",
    "match": "<i>matchString</i>"
}
</pre>

Notes:

- `relativeFolderName` is relative to the user's data folder.

For example:

<pre>
{
    "folder" : "AzureDM",
    "match": "*2017*"
}
</pre>

#### Output

<pre>
{
    "fileName00" : "<i>file size in megabytes</i>",
    "fileName01" : "<i>file size in megabytes</i>"
}
</pre>

### windows.uploadFile

Call this method to upload a saved file to Azure Storage.

#### Input

<pre>
{
    "fileName" : "<i>relativeFileName</i>",
    "connectionString": "<i>connectionString</i>",
    "containerName": "<i>containerName</i>"
}
</pre>

Notes:

- `relativeFolderName` is relative to the user's data folder.

#### Output

<pre>
{
    "errorCode": 0 | <i>errorCode</i>,
    "errorMessage": "<i>errorMessage</i>"
}
</pre>

Notes:

- `"errorCode"` 0 means success - otherwise, it is an error.
- The upload is asynchronous. So, the file should appear on Azure Storage sometime later - or an error is reported to the device twin.

#### windows.deleteFile

Call this method to delete a saved file from the device storage.

#### Input

<pre>
{
    "fileName" : "<i>relativeFileName</i>"
}
</pre>

Notes:

- `relativeFolderName` is relative to the user's data folder.

#### Output

<pre>
{
    "errorCode": 0 | <i>errorCode</i>,
    "errorMessage": "<i>errorMessage</i>"
}
</pre>

Notes:

- `"errorCode"` 0 means success - otherwise, it is an error.
- The delete is asynchronous. So, the file should be removed sometime later.

