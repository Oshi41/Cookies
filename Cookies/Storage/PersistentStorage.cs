using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cookies.Storage
{
    public class PersistentStorage : IPersistantStorage
    {
        private readonly string _path;
        private Lazy<Task<List<Cookie>>> _loadFromInner;

        public PersistentStorage(IStorage inner, string path)
        {
            _path = path;
            _loadFromInner = new Lazy<Task<List<Cookie>>>(inner.GetCookies);

            if (!File.Exists(GetFile()))
            {
                File.CreateText(GetFile()).Close();
            }
        }
        
        public async Task<List<Cookie>> GetCookies()
        {
            var cookies = new List<Cookie>(Get());

            if (!_loadFromInner.IsValueCreated)
            {
                cookies.AddRange(await _loadFromInner.Value);
            }
            
            Set(cookies);

            return cookies;
        }

        public string GetFile()
        {
            return _path;
        }
        
        private void Set(List<Cookie> cookies)
        {
            var json = JsonConvert.SerializeObject(cookies.ToList());
            File.WriteAllText(_path, json);
        }

        private Cookie[] Get()
        {
            var json = File.ReadAllText(GetFile());
            return JsonConvert.DeserializeObject<Cookie[]>(json);
        }

        public void Add(IEnumerable<Cookie> cookies)
        {
            var toAdd = cookies?.ToList();
            if (toAdd?.Any() != true) 
                return;
            
            
            var result = GetCookies().Result;
            result.AddRange(toAdd);
            Set(result);
        }
    }
}