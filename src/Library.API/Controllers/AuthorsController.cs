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

// Auto-mapper has to be used to map between DTOs and Entities

namespace Library.API.Controllers
{   [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository      _libraryRepository      ; 
        private IUrlHelper              _urlHelper              ; 
        private IPropertyMappingService _propertyMappingService ; 
        private ITypeHelperService      _typeHelperService      ; 

        public AuthorsController(ILibraryRepository      libraryRepository
                               , IUrlHelper              urlHelper              
                               , IPropertyMappingService propertyMappingService 
                               , ITypeHelperService      typeHelperService      )
        {
            
            _libraryRepository      = libraryRepository;        //Injects an instance of the repository
            _urlHelper              = urlHelper;                //Injects an instance of UrlHelper
            _propertyMappingService = propertyMappingService;
            _typeHelperService      = typeHelperService;
        }   
        //todo ******************** GET Authors ***********************************
        [HttpGet(Name = "GetAuthors")]       
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters,
                                        [FromHeader(Name ="Accept")] string mediaType)
        {//IActionResult defines a contract that represents the result of an action method

            // Check for invalid orderby and return the correct status code 400
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author> (authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }
            
            // authorsFromRepo is now a page list of author   
            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);

            // Map from Author Entity to Author Dto
            var authors = Mapper.Map<IEnumerable < AuthorDto >> (authorsFromRepo);


            //! HATEOAS is returned only if it requested explicity in the request Header
            if (mediaType == "application/vnd.marvin.hateoas+json")
            {

                // Create metadata for X-pagination
                var paginationMetadata = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages,
                    //x previousPageLink = previousPageLink ,               
                    //x nextPageLink     = nextPageLink                                          
                };

                //Links for author resource
                var links = CreateLinksForAuthors(authorsResourceParameters,
                authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);

                //shaped authors themselves
                var shapedAuthors = authors.ShapeData(authorsResourceParameters.Fields);

                //links for each of those shaped authors
                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    var authorLinks = CreateLinksForAuthor(
                        (Guid)authorAsDictionary["Id"], authorsResourceParameters.Fields);

                    authorAsDictionary.Add("links", authorLinks);

                    return authorAsDictionary;
                });

                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    linkd = links
                };
                //Serialize the result as JSON
                return Ok(linkedCollectionResource);
                //return new JsonResult(authors); // JsonResult returns the given object as JSON
            }
            //! If HATEOS links is not requested then the data will be returned without HATEOAS links
            else
            {
                // Here we can check if we there is a previous page available
                var previousPageLink = authorsFromRepo.HasPrevious ?
                        CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;

                // Here we can check if we there is a next page available
                var nextPageLink = authorsFromRepo.HasNext ?
                        CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;

                var paginationMetadata = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages,
                    previousPageLink = previousPageLink ,               
                    nextPageLink     = nextPageLink                                          
                };

                //Create a custom header
                Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                return Ok(authors.ShapeData(authorsResourceParameters.Fields));
            }
        }
        //todo /////////////////////////////////////////////////////////////////////
        
        //todo ********************* Create Links **********************************
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
                            fields      = authorsResourceParameters . Fields      ,
                            orderBy     = authorsResourceParameters . OrderBy     ,     
                            searchQuery = authorsResourceParameters . SearchQuery ,     
                            genre       = authorsResourceParameters . Genre       ,     
                            pageNumber  = authorsResourceParameters . PageNumber  - 1 , 
                            pageSize    = authorsResourceParameters . PageSize                 
                        });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields      = authorsResourceParameters . Fields      ,
                            orderBy     = authorsResourceParameters . OrderBy     ,     
                            searchQuery = authorsResourceParameters . SearchQuery ,     
                            genre       = authorsResourceParameters . Genre       ,     
                            pageNumber  = authorsResourceParameters . PageNumber  + 1 , 
                            pageSize    = authorsResourceParameters . PageSize                    
                        });
                case ResourceUriType.Current:
                default:
                    return _urlHelper.Link("GerAuthors",
                        new
                        {
                            fields      = authorsResourceParameters . Fields      ,
                            orderBy     = authorsResourceParameters . OrderBy     , 
                            searchQuery = authorsResourceParameters . SearchQuery , 
                            genre       = authorsResourceParameters . Genre       , 
                            pageNumber  = authorsResourceParameters . PageNumber  , 
                            pageSize    = authorsResourceParameters . PageSize            
                        });
            }
        }
        //todo //////////////////////////////////////////////////////////////////////

        //todo ******************** GET One Author **********************************
        [HttpGet("{id}", Name ="GetAuthor")]    //[FromQuery] The values are coming from the query string in the URI sent
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields) //IActionResult defines a contract that represents the result of an action method
        {
            if (!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            var authorFromRepo = _libraryRepository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            // HATEOS Links
            var links = CreateLinksForAuthor(id, fields);

            var linkedResourceToReturn = author.ShapeData(fields)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn); //Serialize the result as JSON

            //return new JsonResult(author); // JsonResult returns the given object as JSON
        }
        //todo //////////////////////////////////////////////////////////////////////

        //todo ******************** POST Create One Author **************************
        [HttpPost(Name = "CreateAuthor")]                     //[FromBody is used to serialize the data from the request into the specified type
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author,
                                        [FromHeader(Name = "Accept")] string mediaType)
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

            //! HATEOAS is returned only if it requested explicity in the request Header
            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                var links = CreateLinksForAuthor(authorToReturn.Id, null);

                var linkedResourceToReturn = authorToReturn.ShapeData(null)
                    as IDictionary<string, object>;

                linkedResourceToReturn.Add("links", links);

                // Call the above get method using its name
                return CreatedAtRoute("GetAuthor",
                    new { id = linkedResourceToReturn["Id"] },
                    linkedResourceToReturn);
            }
            else
            {
                return Ok(authorToReturn);
            }
        }
        //todo //////////////////////////////////////////////////////////////////////

        //todo ******************* Block Creating existing Author *******************
        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);

            return NotFound();
        }
        //todo //////////////////////////////////////////////////////////////////////

        //todo ******************* DELETE One Author ********************************
        [HttpDelete("{authorid}", Name = "DeleteAuthor")]
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
        //todo /////////////////////////////////////////////////////////////////////

        //todo ******************* Create Links for One Author ********************************
        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(
                  new LinkDto(_urlHelper.Link("GetAuthor", new { id = id }),
                  "self",
                  "GET"));
            }
            else
            {
                links.Add(
                  new LinkDto(_urlHelper.Link("GetAuthor", new { id = id, fields = fields }),
                  "self",
                  "GET"));
            }

            links.Add(
              new LinkDto(_urlHelper.Link("DeleteAuthor", new { id = id }),
              "delete_author",
              "DELETE"));

            links.Add(
              new LinkDto(_urlHelper.Link("CreateBookForAuthor", new { authorId = id }),
              "create_book_for_author",
              "POST"));

            links.Add(
               new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { authorId = id }),
               "books",
               "GET"));

            return links;
        }
        //todo /////////////////////////////////////////////////////////////////////

        //todo ******************* Create Links for Authors ********************************
        private IEnumerable<LinkDto> CreateLinksForAuthors(
            AuthorsResourceParameters authorsResourceParameters,
            bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();

            // self 
            links.Add(
               new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
               ResourceUriType.Current)
               , "self", "GET"));

            if (hasNext)
            {
                links.Add(
                  new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                  ResourceUriType.NextPage),
                  "nextPage", "GET"));
            }

            if (hasPrevious)
            {
                links.Add(
                    new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.PreviousPage),
                    "previousPage", "GET"));
            }

            return links;
        }
        //todo /////////////////////////////////////////////////////////////////////

    }
}
