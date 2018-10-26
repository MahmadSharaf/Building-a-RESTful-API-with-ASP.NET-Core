using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{//This class is for dedicating the availability for previous and next values
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; private set; } // it is set to private in order not be manipulated outside the class 
        public int TotalPages { get; private set; } // leading to an invalid value for current page
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public  bool HasPrevious
        {
            get
            {
                return (CurrentPage > 1);
            }            
        }
        public bool HasNext
        {
            get
            {
                return (CurrentPage < TotalPages);
            }
        }

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {//The constructor won't be called directly a static method will be used instead
            TotalCount  = count      ; 
            PageSize    = pageSize   ; 
            CurrentPage = pageNumber ; 
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items); // AddRange is a method on list of T, Which adds items to the underlying list.
        }

        public static PagedList<T>  Create (IQueryable<T> source, int pageNumber, int pageSize)
        {// This class will create this page list for us. This allows is to call pagelist.create, passing an IQuerable,
            // which is exactly what we get after applying the order by class in our repository.
            // All casting and calculations can be made here, rather than having to do that before creating the page list 
            var count = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }


    }
}
