using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Model;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorCollectionController:Controller
    {
        private ILibraryRepository _libraryRepository;

        public AuthorCollectionController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }
        //todo *********** POST Create Author Collection **********************
        [HttpPost()]
        public IActionResult CreateAuthorCollection(
            [FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            if (authorCollection == null)
                return BadRequest();

            var authorEntities = Mapper.Map<IEnumerable<Author>>(authorCollection);
            foreach (var author in authorEntities)
            {
                _libraryRepository.AddAuthor(author);
            }

            if (!_libraryRepository.Save())
                throw new Exception("Creating an author failed on save");

            var authorCollectionToReturn = Mapper.Map < IEnumerable < AuthorDto >> (authorEntities);
            var idsAsString = string.Join(",",
                authorCollectionToReturn.Select(a => a.Id));

            return CreatedAtRoute("GetAuthorCollection",new
            { ids = idsAsString }, authorCollectionToReturn);
        }
        //todo //////////////////////////////////////////////////////////////////

        //todo ******************** GET Author Collection ***************************
        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
                return BadRequest();

            var authorEntities = _libraryRepository.GetAuthors(ids);

            if (ids.Count() != authorEntities.Count())
                return NotFound();

            var authorsToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            return Ok(authorsToReturn);
        }
        //todo ///////////////////////////////////////////////////////////////////
    }
}
