using AutoMapper;
using Library.API.Entities;
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
    public class BooksController : Controller
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

        [HttpGet("{bookId}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid bookId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);

            if (bookForAuthorFromRepo == null)
                return NotFound();
            var book = Mapper.Map<BookDTO>(bookForAuthorFromRepo);

            return Ok(book);
        }

        [HttpPost()]
        public IActionResult CreateAuthorBook(Guid authorId, [FromBody] BookForCreationDTO book)
        {
            if (book == null)
                return BadRequest();
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            var bookEntity = Mapper.Map<Book>(book);

            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            if (!_libraryRepository.Save())
                throw new Exception($"Creating a book for author {authorId} failed on save");

            var bookToReturn = Mapper.Map<BookDTO>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor",
                new { authorId = authorId, bookId = bookToReturn.Id },//the values required for the routing
                bookToReturn);//the content that will be returned by the response body
        }

        [HttpDelete("{bookId}")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid bookId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);
            if (bookForAuthorFromRepo == null)
                return NotFound();
            _libraryRepository.DeleteBook(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"The Book with ID {bookId} for author {authorId} failed on saving");

            return NoContent();
        }

        [HttpPut("{bookId}")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid bookId,
            [FromBody] BookForUpdate book)
        {
            if (book == null)
                return BadRequest();

            var bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);
            if (bookForAuthorFromRepo == null)
                return NotFound();

            Mapper.Map(book, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Updating book {bookId} for author {authorId} failed on saving");

            return NoContent();
         
        }
    }
}
