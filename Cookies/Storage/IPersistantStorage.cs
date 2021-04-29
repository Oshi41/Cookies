using System.Collections.Generic;
using System.Net;

namespace Cookies.Storage
{
    public interface IPersistantStorage : IStorage
    {
        string GetFile();

        void Set(IEnumerable<Cookie> cookies);
    }
}