using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    public class AuthorsResourceParameters
    {
        //Variable for the maximum number of items per page
        const int maxPageSize = 20;
        private int _pageSize=10;

        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                // if page size is larger than that of the maximum size allowed 
                //then the page size will be the maximum size allowed, otherwise page size requested will be allowed.
                _pageSize = (value > maxPageSize) ? maxPageSize : value;
            }
        }

        public string Genre { get; set; }

        public string SearchQuery { get; set; }

        public string OrderBy { get; set; } = "Name";
    }
}
