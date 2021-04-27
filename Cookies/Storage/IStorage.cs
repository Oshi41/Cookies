using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Cookies.Storage
{
    /**
     * Интерфейс доступа к хранилищу.
     * Разные браузеры используют разные хранилища
     */
    public interface IStorage
    {
        /**
         * Возвращает полный список куков для хранилища.
         */
        Task<List<Cookie>> GetCookies();
    }
}