using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using BooksAPI.Data;
using BooksAPI.Models;
using BooksAPI.DTOs;
using System.Linq.Expressions;

namespace BooksAPI.Controllers
{
    [RoutePrefix("api/books")]
    public class BooksController : ApiController
    {
        private BooksAPIContext db = new BooksAPIContext();

        private static readonly Expression<Func<Book, BookDto>> getBookDto =
            x => new BookDto { Author = x.Author.Name, Genre = x.Genre, Title = x.Title };

        // GET: api/Books
        [Route("")]
        public IQueryable<BookDto> GetBooks()
        {
            //return db.Books.Select(x => getBookDto(x));
            return db.Books.Include(x => x.Author).Select(getBookDto);
        }

        // GET: api/Books/5
        [Route("{id:int}")]
        [ResponseType(typeof(BookDto))]
        public async Task<IHttpActionResult> GetBook(int id)
        {
            BookDto bookDto = await db.Books.Include(x => x.Author)
                                    .Where(a => a.BookId == id)
                                    .Select(getBookDto)
                                    .FirstOrDefaultAsync();

            if (bookDto == null)
            {
                return NotFound();
            }
            
            return Ok(bookDto);
        }

        [Route("{id:int}/details")]
        [ResponseType(typeof(BookDetailDto))]
        public async Task<IHttpActionResult> GetBookDetail(int id)
        {
            var book = await (from b in db.Books.Include(b => b.Author)
                              where b.BookId == id
                              select new BookDetailDto
                              {
                                  Title = b.Title,
                                  Genre = b.Genre,
                                  PublishDate = b.PublishDate,
                                  Price = b.Price,
                                  Description = b.Description,
                                  Author = b.Author.Name
                              }).FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound();
            }
            return Ok(book);

        }

        [Route("{genre}")]
        public IQueryable<BookDto> GetBookByGenre(string genre)
        {
            var bookByGenre = db.Books.Include(b => b.Author)
                        .Where(g => g.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase))
                        .Select(getBookDto);

            return bookByGenre;
        }

        [Route("~/api/author/{authorId:int}/book")]
        public IQueryable<BookDto> GetBookByAuthor(int authorId)
        {
            var bookByAuthor = db.Books.Include(b => b.Author)
                        .Where(g => g.AuthorId == authorId)
                        .Select(getBookDto);

            return bookByAuthor;
        }

        [Route("date/{pubdate:datetime}")]
        public IQueryable<BookDto> GetBooks(DateTime pubdate)
        {
            return db.Books.Include(b => b.Author)
                .Where(b => DbFunctions.TruncateTime(b.PublishDate) == DbFunctions.TruncateTime(pubdate))
                .Select(getBookDto);
        }

        // PUT: api/Books/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutBook(int id, Book book)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != book.BookId)
            {
                return BadRequest();
            }

            db.Entry(book).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Books
        [ResponseType(typeof(Book))]
        public async Task<IHttpActionResult> PostBook(Book book)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Books.Add(book);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = book.BookId }, book);
        }

        // DELETE: api/Books/5
        [ResponseType(typeof(Book))]
        public async Task<IHttpActionResult> DeleteBook(int id)
        {
            Book book = await db.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            db.Books.Remove(book);
            await db.SaveChangesAsync();

            return Ok(book);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool BookExists(int id)
        {
            return db.Books.Count(e => e.BookId == id) > 0;
        }
    }
}