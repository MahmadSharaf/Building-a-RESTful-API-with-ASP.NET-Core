using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Model;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Auto-mapper has to be used to map between DTOs and Entities because implement validation attributes or data annotations used to validate input on a class that returns data does not make sense

namespace Library.API.Controllers
{   [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository _libraryRepository;
        private IUrlHelper _urlHelper;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper)
        {
            //Injects an instance of the repository
            _libraryRepository = libraryRepository;
            //Injects an instance of UrlHelper
            _urlHelper = urlHelper;
        }   

        [HttpGet(Name = "GetAuthors")]                    //[FromQuery] The values are coming from the query string in the URI sent
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters) //IActionResult defines a contract that represents the result of an action method
        {   // authorsFromRepo is now a page list of author   
            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);

            // Here we can check if we there is a previous page available
            var previousPageLink = authorsFromRepo.HasPrevious ?
                CreateAuthorsResourceUri(authorsResourceParameters,
                ResourceUriType.PreviousPage) : null;
            // Here we can check if we there is a next page available
            var nextPageLink = authorsFromRepo.HasNext ?
                CreateAuthorsResourceUri(authorsResourceParameters,
                ResourceUriType.NextPage) : null;
            // Create a metadata
            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };
            //Create a custom header
            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            var authors = Mapper.Map<IEnumerable < AuthorDto >> (authorsFromRepo);

            //Serialize the result as JSON
            return Ok(authors);
            //return new JsonResult(authors); // JsonResult returns the given object as JSON
        }

        // Create Links
        private string CreateAuthorsResourceUri(
            AuthorsResourceParameters authorsResourceParameters, //accepts the old resource parameter, as it is needed to generate URIs
            ResourceUriType type) //It also accept enumeration, so we can pass in whatever we want to generate a previous or next page link 
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                default:
                    return _urlHelper.Link("GerAuthors",
                        new
                        {
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                        });
            }
        }

        [HttpGet("{id}", Name ="GetAuthor")]
        public IActionResult GetAuthor(Guid id) //IActionResult defines a contract that represents the result of an action method
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            var author = Mapper.Map<AuthorDto>(authorFromRepo);
              
            return Ok(author);
            //Serialize the result as JSON
            //return new JsonResult(author); // JsonResult returns the given object as JSON
        }

        [HttpPost]                     //[FromBody is used to serialize the data from the request into the specified type
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
            {
                return BadRequest();
            }
            var authorEntity = Mapper.Map<Author>(author);

            //The author hasn't been added to the database yet,it has been added to the Dbcontext,
            //which represent a session with the database, we must call save to the repository
            _libraryRepository.AddAuthor(authorEntity);

            if(!_libraryRepository.Save())
            {
                //Throwing exception is expensive it hits performance, but it is better we can use a global error message
                throw new Exception("Creating an author failed on save");
                
                // This approach is good, but we have code to return internal server errors in different places, it will be a problem while logging also
                //return StatusCode(500, "A problem happened with handling the request.")
            }

            // The Response will be the author just added but from the database itself after being added
            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

                                // Call the above get method using its name
            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }/*?!new*/,
                authorToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);

            return NotFound();
        }

        [HttpDelete("{authorid}")]
        public IActionResult DeleteAuthor(Guid authorId)
        {
            if (authorId == null)
                return NotFound();

            var authorToDelete = _libraryRepository.GetAuthor(authorId);

            if (authorId == null)
                return NotFound();
            _libraryRepository.DeleteAuthor(authorToDelete);
            if (!_libraryRepository.Save())
                throw new Exception($"The author with ID {authorId} failed while saving");

            return NoContent();
        }
    }
}
