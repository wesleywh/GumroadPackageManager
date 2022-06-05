using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

namespace RestAPI
{
    public class ResponseObject
    {
        public string response = null;
        public HttpStatusCode code;
        public WebHeaderCollection headers = null;
    }
    public class RestAPI
    {
        public virtual ResponseObject GET(string url, Dictionary<string, string> headers)
        {
            return Base(url, "GET", headers);
        }
        public virtual ResponseObject DELETE(string url, Dictionary<string, string> headers)
        {
            return Base(url, "DELETE", headers);
        }
        public virtual ResponseObject PUT(string url, Dictionary<string, string> headers)
        {
            return Base(url, "PUT", headers);
        }
        public virtual ResponseObject POST(string url, Dictionary<string, string> headers)
        {
            return Base(url, "POST", headers);
        }

        protected virtual ResponseObject Base(string url, string callType, Dictionary<string, string> headers)
        {
            ResponseObject jsonResponse = new ResponseObject();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                foreach (KeyValuePair<string, string> item in headers)
                {
                    if (item.Key == "Host")
                        request.Host = item.Value;
                    else
                        request.Headers.Add(item.Key, item.Value);
                }
                request.Method = callType;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    jsonResponse.code = response.StatusCode;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        jsonResponse.response = reader.ReadToEnd();
                        jsonResponse.headers = response.Headers;
                    }
                    else
                    {
                        jsonResponse.response = response.StatusDescription;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                Debug.Log($"Failed - {callType} - To: {url}");
            }
            return jsonResponse;
        }
    }
}