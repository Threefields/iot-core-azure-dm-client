#include "stdafx.h"
#include <windows.h>
#include <TraceLoggingProvider.h>
#include "ETWLogger.h"

TRACELOGGING_DEFINE_PROVIDER(gLogProvider, PROVIDER_NAME, PROVIVDER_GUID);

using namespace std;

namespace Utils
{
    ETWLogger::ETWLogger()
    {
        TraceLoggingRegister(gLogProvider);
    }

    ETWLogger::~ETWLogger()
    {
        TraceLoggingUnregister(gLogProvider);
    }

    void ETWLogger::Log(const std::wstring& msg, LoggingLevel level)
    {
        switch (level)
        {
        case Verbose:
            TraceLoggingWrite(gLogProvider, "LogMsgVerbose", TraceLoggingWideString(msg.c_str(), "msg"));
            break;
        case Information:
            TraceLoggingWrite(gLogProvider, "LogMsgInformation", TraceLoggingWideString(msg.c_str(), "msg"));
            break;
        case Warning:
            TraceLoggingWrite(gLogProvider, "LogMsgWarning", TraceLoggingWideString(msg.c_str(), "msg"));
            break;
        case Error:
            TraceLoggingWrite(gLogProvider, "LogMsgError", TraceLoggingWideString(msg.c_str(), "msg"));
            break;
        case Critical:
            TraceLoggingWrite(gLogProvider, "LogMsgCritical", TraceLoggingWideString(msg.c_str(), "msg"));
            break;
        }
    }

    void ETWLogger::Log(const std::string& msg, LoggingLevel level)
    {
        switch (level)
        {
        case Verbose:
            TraceLoggingWrite(gLogProvider, "LogMsgVerbose", TraceLoggingString(msg.c_str(), "msg"));
            break;
        case Information:
            TraceLoggingWrite(gLogProvider, "LogMsgInformation", TraceLoggingString(msg.c_str(), "msg"));
            break;
        case Warning:
            TraceLoggingWrite(gLogProvider, "LogMsgWarning", TraceLoggingString(msg.c_str(), "msg"));
            break;
        case Error:
            TraceLoggingWrite(gLogProvider, "LogMsgError", TraceLoggingString(msg.c_str(), "msg"));
            break;
        case Critical:
            TraceLoggingWrite(gLogProvider, "LogMsgCritical", TraceLoggingString(msg.c_str(), "msg"));
            break;
        }
    }
}