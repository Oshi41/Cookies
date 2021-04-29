using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Cookies.Storage;

namespace Cookies
{
    public class HttpHelper
    {
        private readonly Func<IStorage, Task<HttpClientHandler>> _getHandler;
        private readonly IStorage _storage;
        private readonly FieldInfo _hashTableField;
        private FieldInfo _property;


        public HttpHelper(IStorage storage, Func<IStorage, Task<HttpClientHandler>> getHandler)
        {
            _storage = storage;
            _getHandler = getHandler;

            if (storage is IPersistantStorage)
            {
                _hashTableField = typeof(CookieContainer).GetField("m_domainTable", BindingFlags.NonPublic |
                                                                           BindingFlags.GetField |
                                                                           BindingFlags.Instance);
                
            }
        }

        protected CancellationToken CreateToken(TimeSpan? span)
        {
            return span == null
                ? CancellationToken.None
                : new CancellationTokenSource(span.Value).Token;
        }

        protected virtual void SaveCookies(HttpClientHandler handler)
        {
            if (_hashTableField == null)
                return;

            if (handler?.CookieContainer == null)
                return;
            
            if (!(_storage is IPersistantStorage persistant))
                return;

            if (_hashTableField.GetValue(handler.CookieContainer) is Hashtable table)
            {
                var list = new List<Cookie>();
            
                foreach (var key in table.Keys)
                {
                    var item = table[key];

                    if (_property == null)
                    {
                        _property = item.GetType().GetField("m_list", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    
                    if (_property?.GetValue(item) is SortedList sortedList)
                        list.AddRange(sortedList.Values.OfType<Cookie>());
                }
            
                persistant.Add(list);
            }
        }

        public virtual async Task<string> PostAsync(
            string endPoint,
            Dictionary<string, string> parameters,
            TimeSpan? maxTime = null
        )
        {
            using var handler = await _getHandler(_storage);
            using var client = new HttpClient(handler);
            var content = new FormUrlEncodedContent(parameters);
            using var response = await client.PostAsync(endPoint, content, CreateToken(maxTime));
            if (response.IsSuccessStatusCode)
                SaveCookies(handler);
            
            return await response.Content.ReadAsStringAsync();
        }

        public virtual async Task<(
                HttpStatusCode StatusCode,
                string reqHeaders,
                string resHeaders,
                string ResponseContent
                )>
            GetAsync(string endPoint, Dictionary<string, string> parameters, TimeSpan? maxTime = null)
        {
            var builder = new UriBuilder(endPoint);
            builder.Port = -1;

            var query = HttpUtility.ParseQueryString(builder.Query);
            parameters.ToList().ForEach(v => query[v.Key] = v.Value);
            builder.Query = query.ToString();

            var url = builder.ToString();

            using var handler = await _getHandler(_storage);
            using var client = new HttpClient(handler);
            using var response = await client.GetAsync(url, CreateToken(maxTime));
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                SaveCookies(handler);
            SaveCookies(handler);
                
            return (
                StatusCode: response.StatusCode,
                reqHeaders: client.DefaultRequestHeaders.ToString(),
                resHeaders: response.Headers.ToString(),
                ResponseContent: responseContent);
        }
    }
}