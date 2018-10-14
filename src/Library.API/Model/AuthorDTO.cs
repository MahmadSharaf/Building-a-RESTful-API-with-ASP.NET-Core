using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


// DTOs should be used as the Outer Facing Model instead of Entity Directly. Because we may need to changed the table fields or concatenate fields together

namespace Library.API.Model
{
    public class AuthorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Genre { get; set; }
    }
}
