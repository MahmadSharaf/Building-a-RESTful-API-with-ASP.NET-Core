using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Model;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
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

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDTO),
                    "The provided description should be different from the title.");
            }

            //Model State is a dictionary contains both of the model and model binding validation, 
            //it also contains a collection of error messages for each value submitted
            if(!ModelState.IsValid)
            {//Validation
                return new UnprocessableEntityObjectResult(ModelState);
            }

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
            [FromBody] BookForUpdateDTO book)
        {
            if (book == null)
                return BadRequest();

            var bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);
            if (bookForAuthorFromRepo == null)//upserting
            {   //Create a variable that is mapped from type Book and the variable used is book because we want the data that in the request body
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = bookId;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);
                if(!_libraryRepository.Save())
                    throw new Exception($"Upserting book {bookId}, for author {authorId} failed on saving");

                var bookToReturn = Mapper.Map<BookDTO>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor",
                    new { authorId = authorId, bookId, bookToReturn.Id }
                    , bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Updating book {bookId} for author {authorId} failed on saving");

            return NoContent();
         
        }

        [HttpPatch("{bookId}")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid bookId,
            [FromBody] JsonPatchDocument<BookForUpdateDTO> patchDoc)//JsonPatchDocument is used because the content header is Json-patch
        {
            if (patchDoc == null)
                return BadRequest();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);

            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDTO();
                patchDoc.ApplyTo(bookDto); //To apply the changes to the bookDto

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = bookId;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!_libraryRepository.Save())
                    throw new Exception($"Upserting Book {bookId} for author {authorId} failed while saving");

                var bookToReturn = Mapper.Map<BookDTO>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor",
                    new { authorId = authorId, bookId, bookToReturn.Id },
                    bookToReturn);
            }

            var bookToPatch = Mapper.Map<BookForUpdateDTO>(bookForAuthorFromRepo);

            patchDoc.ApplyTo(bookToPatch);

            //addValidation

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Patching book {bookId} for author {authorId} failed on saving");

            return NoContent();
        }
    }
}
