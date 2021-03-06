﻿using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Model;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILogger<BooksController> _logger;
        private ILibraryRepository _libraryRepository;
        private IUrlHelper _urlHelper;

        public BooksController(ILibraryRepository libraryRepository,
                               ILogger<BooksController> logger,
                               IUrlHelper urlHelper)
        {    
            _logger = logger; //Loggers can also be injected in the constructor not only in the loggerFactory in startup.cs
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
        }

        //todo *************** Get Books For Author ***************************
        [HttpGet(Name = "GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var books = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            books = books.Select(book => //Change each book from book to book with links
            {
                book = CreateLinksForBook(book);
                return book;
            });

            // Create links
            var wrapper = new LinkedCollectionResourceWrapperDto<BookDto>(books);

            return Ok(CreateLinksForBooks(wrapper));
        }
        //todo ////////////////////////////////////////////////////////////////

        //todo *************** Get one Book For Author ***************************
        [HttpGet("{bookId}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid bookId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);

            if (bookForAuthorFromRepo == null)
                return NotFound();
            var book = Mapper.Map<BookDto>(bookForAuthorFromRepo);

            return Ok(CreateLinksForBook(book));
        }
        //todo ////////////////////////////////////////////////////////////////

        //todo *************** POST Create Author Book ***************************
        [HttpPost(Name = "CreateBookForAuthor")]
        public IActionResult CreateAuthorBook(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
                return BadRequest();

            //Cutom validation error
            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDto),
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

            var bookToReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor",
                new { authorId = authorId, bookId = bookToReturn.Id },//the values required for the routing
                CreateLinksForBook(bookToReturn));//the content that will be returned by the response body
        }
        //todo ////////////////////////////////////////////////////////////////

        //todo *************** Delete one book for author ***************************
        [HttpDelete("{bookId}", Name = "DeleteBookForAuthor")]
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

            _logger.LogInformation(100, $"Book {bookId} for author {authorId} was deleted");
            
            return NoContent();
        }
        //todo ////////////////////////////////////////////////////////////////

        //todo *************** PUT Update Book For Author ***************************
        [HttpPut("{bookId}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid bookId,
            [FromBody] BookForUpdateDto book)
        {
            if (book == null)
                return BadRequest();

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            //Model State is a dictionary contains both of the model and model binding validation, 
            //it also contains a collection of error messages for each value submitted
            if (!ModelState.IsValid)
            {//Validation
                return new UnprocessableEntityObjectResult(ModelState);
            }

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

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

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
        //todo ////////////////////////////////////////////////////////////////

        //todo *************** PATCH Partially Update Book For Author ***************************
        [HttpPatch("{bookId}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid bookId,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)//JsonPatchDocument is used because the content header is Json-patch
        {
            if (patchDoc == null)
                return BadRequest();

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, bookId);

            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDto();

                patchDoc.ApplyTo(bookDto, ModelState); //To apply the changes to the bookDto

                if (bookDto.Description == bookDto.Title)
                {
                    ModelState.AddModelError(nameof(BookForUpdateDto),
                        "The provided description should be different from the title.");
                }
                                
                TryValidateModel(bookDto);

                if (!ModelState.IsValid)
                {//Validation
                    return new UnprocessableEntityObjectResult(ModelState);
                }

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = bookId;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!_libraryRepository.Save())
                    throw new Exception($"Upserting Book {bookId} for author {authorId} failed while saving");

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor",
                    new { authorId = authorId, bookId, bookToReturn.Id },
                    bookToReturn);
            }

            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            //patchDoc.ApplyTo(bookToPatch, ModelState);
            patchDoc.ApplyTo(bookToPatch);

            if (bookToPatch.Description == bookToPatch.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            TryValidateModel(bookToPatch);

            //Model State is a dictionary contains both of the model and model binding validation, 
            //it also contains a collection of error messages for each value submitted
            if (!ModelState.IsValid)
            {//Validation
                return new UnprocessableEntityObjectResult(ModelState);
            }

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Patching book {bookId} for author {authorId} failed on saving");

            return NoContent();
        }
        //todo ////////////////////////////////////////////////////////////////

        //todo *************** Create Links for a book ************************
        private BookDto CreateLinksForBook(BookDto book)
        {
            book.Links.Add(new LinkDto(_urlHelper.Link("GetBookForAuthor", 
                            new { id = book.Id }),
                            "self", "GET"));
            book.Links.Add(new LinkDto(_urlHelper.Link("DeleteBookForAuthor", 
                            new { id = book.Id }),
                            "delete_book", "DELETE"));
            book.Links.Add(new LinkDto(_urlHelper.Link("UpdateBookForAuthor", 
                            new { id = book.Id }),
                            "update_book", "PUT"));
            book.Links.Add(new LinkDto(_urlHelper.Link("PartiallyUpdateBookForAuthor", 
                            new { id = book.Id }),
                            "partially_update_book", "PATCH"));
            return book;
        }
        //todo ////////////////////////////////////////////////////////////////

        //todo *************** Create Links for books  ************************
        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
            LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            booksWrapper.Links.Add(
                new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { }),
                "self", "GET"));

            return booksWrapper;
        }
    }
}
