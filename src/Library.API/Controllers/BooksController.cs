using AutoMapper;
using Library.API.Model;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController:Controller
    {
        private ILibraryRepository _libraryRepository;

        public BooksController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        [HttpGet()]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var books = Mapper.Map<IEnumerable<BookDTO>>(booksForAuthorFromRepo);
            return Ok(books);
        }

        [HttpGet("{bookId}")]
        public IActionResult GetBookForAuthor(Guid authorId,Guid bookId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId,bookId);

            if (bookForAuthorFromRepo == null)
                return NotFound();
            var book = Mapper.Map<BookDTO>(bookForAuthorFromRepo);

            return Ok(book);
        }
    }
}
