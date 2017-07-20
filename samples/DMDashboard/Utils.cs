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

using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace DMDashboard
{
    public class Utils
    {
        public static JToken GetJToken(JObject jObj, string propertyName)
        {
            JToken jValue;
            if (jObj.TryGetValue(propertyName, out jValue))
            {
                return jValue;
            }
            return null;
        }

        public static string GetString(JObject jObj, string propertyName, string defaultValue)
        {
            JToken jValue;
            if (jObj.TryGetValue(propertyName, out jValue))
            {
                if (jValue.Type == JTokenType.String)
                {
                    return (string)jValue;
                }
                else
                {
                    Debug.WriteLine($"Property {propertyName} found but its type is not a string!");
                }
            }
            return defaultValue;
        }

        public static bool GetBool(JObject jObj, string propertyName, bool defaultValue)
        {
            JToken jValue;
            if (jObj.TryGetValue(propertyName, out jValue))
            {
                if (jValue.Type == JTokenType.Boolean)
                {
                    return (bool)jValue;
                }
                else
                {
                    Debug.WriteLine($"Property {propertyName} found but its type is not a boolean!");
                }
            }
            return defaultValue;
        }

    }
}