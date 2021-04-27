using System;
using System.Linq;
using Cookies.Storage;

namespace Cookies
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            IStorage storage = new ChromeStorageDecrypt();

            var cookies = storage.GetCookies().Result;

            var list = cookies.Where(x => x.Domain.Contains("adblockplus.org")).ToList();
        }
    }
}