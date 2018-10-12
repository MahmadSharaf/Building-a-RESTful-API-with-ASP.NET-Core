using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{   [Route("api/authors")]
    public class AuthorsController :Controller
    {
        private ILibraryRepository _libraryRepository;

        public AuthorsController(ILibraryRepository libraryRepository)
        {//Injects an instance of the repository
            _libraryRepository = libraryRepository;
        }

        [HttpGet()]
        public IActionResult GetAuthors() //IActionResult defines a contract that represents the result of an action method
        {
            var authorsFromRepo = _libraryRepository.GetAuthors();
            //Serialize the result as JSON
            return new JsonResult(authorsFromRepo); // JsonResult returns the given object as JSON
        }

        [HttpGet("{id}")]
        public IActionResult GetAuthor(Guid id) //IActionResult defines a contract that represents the result of an action method
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            //Serialize the result as JSON
            return new JsonResult(authorFromRepo); // JsonResult returns the given object as JSON
        }
    }
}
