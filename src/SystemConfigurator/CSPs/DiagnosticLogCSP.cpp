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

#include <stdafx.h>
#include <filesystem>
#include <fstream>
#include <iomanip>
#include "..\SharedUtilities\Logger.h"
#include "MdmProvision.h"
#include "DiagnosticLogCSP.h"

using namespace std;
using namespace Microsoft::Devices::Management::Message;

IResponse^ DiagnosticLogCSP::HandleGetEventTracingConfiguration(IRequest^ request)
{
    TRACE(__FUNCTION__);

    wstring path = L"./Vendor/MSFT/DiagnosticLog/EtwLog/Collectors?list=StructData";

    map<wstring, CollectorReportedConfiguration^> collectors;

    GetEventTracingConfigurationResponse^ response = ref new GetEventTracingConfigurationResponse(ResponseStatus::Success);
    response->Collectors = ref new Vector<CollectorReportedConfiguration^>();

    std::function<void(std::vector<std::wstring>&, std::wstring&)> valueHandler =
        [response, &collectors](vector<wstring>& uriTokens, wstring& value)
    {
        CollectorReportedConfiguration^ currentCollector;

        if (uriTokens.size() < 7)
        {
            return;
        }

        wstring cspCollectorName = uriTokens[6];

// 0/__1___/_2__/______3______/__4___/____5_____/___6___/
// ./Vendor/MSFT/DiagnosticLog/EtwLog/Collectors/AzureDM
map<wstring, CollectorReportedConfiguration^>::iterator it = collectors.find(cspCollectorName);
if (it == collectors.end())
{
    wstring registryPath = IoTDMRegistryEventTracing;
    registryPath += L"\\";
    registryPath += cspCollectorName.c_str();

    currentCollector = ref new CollectorReportedConfiguration();
    currentCollector->Name = ref new String(cspCollectorName.c_str());

    wstring reportToDeviceTwin = Utils::ReadRegistryValue(registryPath, IoTDMRegistryReportToDeviceTwin, L"no");
    currentCollector->ReportToDeviceTwin = ref new String(reportToDeviceTwin.c_str());
    currentCollector->CSPConfiguration->LogFileFolder = ref new String(Utils::ReadRegistryValue(registryPath, IoTDMRegistryEventTracingLogFileFolder, cspCollectorName).c_str());

    response->Collectors->Append(currentCollector);

    collectors[cspCollectorName] = currentCollector;
}
else
{
    currentCollector = it->second;
}

if (uriTokens.size() >= 8)
{
    // 0/__1___/_2__/______3______/__4___/____5_____/___6___/____7______/
    // ./Vendor/MSFT/DiagnosticLog/EtwLog/Collectors/AzureDM/TraceStatus

    if (uriTokens[7] == L"TraceStatus")
    {
        currentCollector->CSPConfiguration->Started = std::stoi(value) == 1 ? L"yes" : L"no";
    }
    else if (uriTokens[7] == L"TraceLogFileMode")
    {
        currentCollector->CSPConfiguration->TraceLogFileMode = std::stoi(value) == 1 ? L"sequential" : L"circular";
    }
    else if (uriTokens[7] == L"LogFileSizeLimitMB")
    {
        currentCollector->CSPConfiguration->LogFileSizeLimitMB = std::stoi(value);
    }
}

if (uriTokens.size() >= 9)
{
    // 0/__1___/_2__/______3______/__4___/____5_____/___6___/____7____/_8__
    // ./Vendor/MSFT/DiagnosticLog/EtwLog/Collectors/AzureDM/Providers/guid

    if (currentCollector->CSPConfiguration->Providers == nullptr)
    {
        currentCollector->CSPConfiguration->Providers = ref new Vector<ProviderConfiguration^>();
    }

    ProviderConfiguration^ currentProvider;
    for (auto provider : currentCollector->CSPConfiguration->Providers)
    {
        if (0 == _wcsicmp(provider->Guid->Data(), uriTokens[8].c_str()))
        {
            currentProvider = provider;
        }
    }

    if (currentProvider == nullptr)
    {
        ProviderConfiguration^ provider = ref new ProviderConfiguration();
        provider->Guid = ref new String(uriTokens[8].c_str());
        currentCollector->CSPConfiguration->Providers->Append(provider);

        currentProvider = provider;
    }

    if (uriTokens.size() >= 10)
    {
        // 0/__1___/_2__/______3______/__4___/____5_____/___6___/___7_____/_8__/____9____
        // ./Vendor/MSFT/DiagnosticLog/EtwLog/Collectors/AzureDM/Providers/guid/TraceLevel

        if (uriTokens[9] == L"TraceLevel")
        {
            currentProvider->TraceLevel = ref new String(value.c_str());
        }
        else if (uriTokens[9] == L"Keywords")
        {
            currentProvider->Keywords = ref new String(value.c_str());
        }
        else if (uriTokens[9] == L"State")
        {
            // ToDo: case senitive!
            currentProvider->Enabled = value == L"true" ? true : false;
        }
    }
}
    };

    MdmProvision::RunGetStructData(path, valueHandler);

    return response;
}

IResponse^ DiagnosticLogCSP::HandleSetEventTracingConfiguration(IRequest^ request)
{
    TRACE(__FUNCTION__);

    // ToDo: There is a bug in the CSP where if the Documents folder
    //       does not exist, it fails to start capturing events.
    //       To work around that, we are making sure the documents 
    //       folder exists.
    Utils::EnsureFolderExists(L"C:\\Data\\Users\\Public\\Documents");

    const wstring cspRoot = L"./Vendor/MSFT/DiagnosticLog/EtwLog/Collectors";

    auto eventTracingConfiguration = dynamic_cast<SetEventTracingConfigurationRequest^>(request);
    for each (CollectorDesiredConfiguration^ collector in eventTracingConfiguration->Collectors)
    {
        TRACE(L"- Collector ------------------------");
        TRACEP(L" ---- Apply  : ", collector->ApplyFromDeviceTwin->Data());
        TRACEP(L" ---- Report : ", collector->ReportToDeviceTwin->Data());
        TRACEP(L" ---- ---- Name               : ", collector->Name->Data());
        MdmProvision::RunAdd(cspRoot, collector->Name->Data());

        if (collector->CSPConfiguration == nullptr)
        {
            TRACE(L"Error: CSPConfiguration is nullptr.");
        }
        else
        {
            const wstring collectorCSPPath = cspRoot + L"/" + collector->Name->Data();

            if (collector->CSPConfiguration->LogFileFolder == nullptr)
            {
                TRACE(L"Error: CSPConfiguration->LogFileFolder is nullptr.");
            }
            else
            {
                TRACEP(L" ---- ---- LogFileFolder      : ", collector->CSPConfiguration->LogFileFolder->Data());
            }

            wstring registryPath = IoTDMRegistryEventTracing;
            registryPath += L"\\";
            registryPath += collector->Name->Data();

            Utils::WriteRegistryValue(registryPath, IoTDMRegistryReportToDeviceTwin, collector->ReportToDeviceTwin->Data());
            Utils::WriteRegistryValue(registryPath, IoTDMRegistryEventTracingLogFileFolder, collector->CSPConfiguration->LogFileFolder->Data());

            MdmProvision::RunSet(collectorCSPPath + L"/LogFileSizeLimitMB", collector->CSPConfiguration->LogFileSizeLimitMB);
            MdmProvision::RunSet(collectorCSPPath + L"/TraceLogFileMode", collector->CSPConfiguration->TraceLogFileMode == L"sequential" ? 1 : 2);

            wstring providersString = MdmProvision::RunGetString(collectorCSPPath + L"/Providers");

            for each (ProviderConfiguration^ provider in collector->CSPConfiguration->Providers)
            {
                wstring providerCSPPath = collectorCSPPath + L"/Providers/" + provider->Guid->Data();

                TRACE(L" ---- ---- Provider ------------------------");
                TRACEP(L" ---- ---- ---- Guid       : ", provider->Guid->Data());
                if (wstring::npos == providersString.find(provider->Guid->Data()))
                {
                    wstring dummyEtl;
                    dummyEtl += SC_CLEANUP_FOLDER;
                    dummyEtl += L"\\DMAddProviderSession.etl";

                    wstring xperfExe = L"C:\\windows\\system32\\xperf.exe";
                    wstring xperfSession = L"DMAddProviderSession";
                    wstring dummyXperfStartCmd;
                    dummyXperfStartCmd += xperfExe;
                    dummyXperfStartCmd += L" -start ";
                    dummyXperfStartCmd += xperfSession;
                    dummyXperfStartCmd += L" -f ";
                    dummyXperfStartCmd += dummyEtl;
                    dummyXperfStartCmd += L" -on ";
                    dummyXperfStartCmd += provider->Guid->Data();

                    unsigned long returnCode = 0;
                    string output;
                    Utils::LaunchProcess(dummyXperfStartCmd, returnCode, output);

                    MdmProvision::RunAddTyped(providerCSPPath, L"node");

                    wstring dummyXperfStopCmd;
                    dummyXperfStopCmd += xperfExe;
                    dummyXperfStopCmd += L" -stop ";
                    dummyXperfStopCmd += xperfSession;
                    Utils::LaunchProcess(dummyXperfStopCmd, returnCode, output);

                    DeleteFile(dummyEtl.c_str());
                }

                int traceLevel = 0;
                if (provider->TraceLevel == L"critical")
                {
                    traceLevel = 1;
                }
                else if (provider->TraceLevel == L"error")
                {
                    traceLevel = 2;
                }
                else if (provider->TraceLevel == L"warning")
                {
                    traceLevel = 3;
                }
                else if (provider->TraceLevel == L"information")
                {
                    traceLevel = 4;
                }
                else if (provider->TraceLevel == L"verbose")
                {
                    traceLevel = 5;
                }

                MdmProvision::RunSet(providerCSPPath + L"/State", provider->Enabled);
                MdmProvision::RunSet(providerCSPPath + L"/Keywords", wstring(provider->Keywords->Data()));
                MdmProvision::RunSet(providerCSPPath + L"/TraceLevel", traceLevel);
            }

            // Finally process the started/stopped status...
            unsigned int traceStatus = MdmProvision::RunGetUInt(collectorCSPPath + L"/TraceStatus");
            if (collector->CSPConfiguration->Started == L"yes")
            {
                if (traceStatus == 0 /*stopped*/)
                {
                    TRACE(L"Should start logging here!");
                    MdmProvision::RunExecWithParameters(collectorCSPPath + L"/TraceControl", L"START");
                }
            }
            else
            {
                if (traceStatus == 1 /*started*/)
                {
                    TRACE(L"Should stop logging here!");
                    MdmProvision::RunExecWithParameters(collectorCSPPath + L"/TraceControl", L"STOP");

                    // ToDo: Make sure the filefolder does not have ..\ or relative path.
                    wstring etlFileName;
                    etlFileName += SC_CLEANUP_FOLDER;
                    etlFileName += L"\\";
                    etlFileName += collector->CSPConfiguration->LogFileFolder->Data();
                    CreateDirectory(etlFileName.c_str(), NULL);

                    etlFileName += L"\\";
                    etlFileName += collector->Name->Data();

                    time_t now;
                    time(&now);

                    tm* nowParsed = nullptr;
                    errno_t errCode = localtime_s(nowParsed, &now);
                    if (errCode != 0)
                    {
                        // ToDo: Throw an error...
                    }

                    basic_ostringstream<wchar_t> nowString;
                    nowString << (nowParsed->tm_year + 1900) << L"_";
                    nowString << setw(2) << setfill(L'0') << (nowParsed->tm_mon + 1) << L"_";
                    nowString << setw(2) << setfill(L'0') << nowParsed->tm_mday << L"_";
                    nowString << setw(2) << setfill(L'0') << nowParsed->tm_hour << L"_";
                    nowString << setw(2) << setfill(L'0') << nowParsed->tm_min << L"_";
                    nowString << setw(2) << setfill(L'0') << nowParsed->tm_sec;

                    etlFileName += L"_";
                    etlFileName += nowString.str();
                    etlFileName += L".etl";

                    TRACEP(L"File Name: ", etlFileName.c_str());

                    wstring collectorFileCSPPath;
                    collectorFileCSPPath += L"./Vendor/MSFT/DiagnosticLog/FileDownload/DMChannel/";
                    collectorFileCSPPath += collector->Name->Data();

                    vector<vector<char>> decryptedEtlBuffer;

                    int blockCount = 0;
                    if (MdmProvision::TryGetNumber(collectorFileCSPPath + L"/BlockCount", blockCount))
                    {
                        for (int i = 0; i < blockCount; ++i)
                        {
                            MdmProvision::RunSet(collectorFileCSPPath + L"/BlockIndexToRead", i);
                            wstring blockData = MdmProvision::RunGetBase64(collectorFileCSPPath + L"/BlockData");

                            vector<char> decryptedBlock;
                            Utils::Base64ToBinary(blockData, decryptedBlock);
                            decryptedEtlBuffer.push_back(decryptedBlock);
                        }
                    }

                    ofstream etlFile(etlFileName, ios::out | ios::binary);
                    for (auto it = decryptedEtlBuffer.begin(); it != decryptedEtlBuffer.end(); it++)
                    {
                        etlFile.write(it->data(), it->size());
                    }
                    etlFile.close();
                }
            }
        }
    }

    return ref new StringResponse(ResponseStatus::Success, ref new String(), DMMessageKind::SetEventTracingConfiguration);
}