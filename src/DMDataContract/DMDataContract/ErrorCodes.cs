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

namespace DMDataContract
{
    public static class ErrorCodes
    {
        // OS Errors
        public static int E_NOTIMPL = unchecked((int)0x80000001);

        // App management error codes 0000-0080
        public static int INVALID_DESIRED_VERSION = unchecked((int)0xA0000000);
        public static int INVALID_DESIRED_PKG_FAMILY_ID = unchecked((int)0xA0000001);
        public static int INVALID_DESIRED_APPX_SRC = unchecked((int)0xA0000002);
        public static int INVALID_DESIRED_APPX_OPERATION = unchecked((int)0xA0000003);
        public static int INVALID_INSTALLED_APP_VERSION_UNCHANGED = unchecked((int)0xA0000004);
        public static int INVALID_INSTALLED_APP_VERSION_UNEXPECTED = unchecked((int)0xA0000005);
        public static int INVALID_DESIRED_MULTIPLE_FOREGROUND_APPS = unchecked((int)0xA0000006);
        public static int INVALID_DESIRED_CONFLICT_UNINSTALL_FOREGROUND_APP = unchecked((int)0xA0000007);

        // DM Storage management error codes 0081-0100
        public static int INVALID_PARAMS = unchecked((int)0xA0000081);
        public static int INVALID_FOLDER_PARAM = unchecked((int)0xA0000082);
        public static int INVALID_FILE_PARAM = unchecked((int)0xA0000083);
        public static int INVALID_CONNECTION_STRING_PARAM = unchecked((int)0xA0000084);
        public static int INVALID_CONTAINER_PARAM = unchecked((int)0xA0000084);
    }
}
