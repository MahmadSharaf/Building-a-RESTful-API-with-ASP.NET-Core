using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    public class AuthorsController :Controller
    {
        private ILibraryRepository _libraryRepository;

        public AuthorsController(ILibraryRepository libraryRepository)
        {//Injects an instance of the repository
            _libraryRepository = libraryRepository;
        }

        public IActionResult GetAuthors() //IActionResult defines a contract that represents the result of an action method
        {
            var authorsFromRepo = _libraryRepository.GetAuthors();
            //Serialize the result as JSON
            return new JsonResult(authorsFromRepo); // JsonResult returns the given object as JSON
        }
    }
}
