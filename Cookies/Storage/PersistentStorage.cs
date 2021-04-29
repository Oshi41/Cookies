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

            if (!File.Exists(path))
            {
                File.CreateText(path).Close();
            }
        }
        
        public async Task<List<Cookie>> GetCookies()
        {
            if (!_loadFromInner.IsValueCreated)
            {
                var cookies = await _loadFromInner.Value;
                Set(cookies);
            }

            var json = await File.ReadAllTextAsync(_path);
            return JsonConvert.DeserializeObject<List<Cookie>>(json);
        }

        public string GetFile()
        {
            return _path;
        }

        public void Set(IEnumerable<Cookie> cookies)
        {
            var json = JsonConvert.SerializeObject(cookies.ToList());
            File.WriteAllText(_path, json);
        }
    }
}