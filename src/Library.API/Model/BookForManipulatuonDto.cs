using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

// A base class is preferly used to minimize the amount of duplicate code
// This is will serve as a base class for DTOs
namespace Library.API.Model
{   // Class declaration "abstract" which means it won't be used to work on its own
    public abstract class BookForManipulatuonDto
    {
        //Validation annotations with error message
        [Required(ErrorMessage = "You should fill out a title.")]
        [MaxLength(100, ErrorMessage = "The title shouldn't have more than 100 characteres")]
        public string Title { get; set; }

        [MaxLength(500, ErrorMessage = "The description shouldn't have more than 500 characteres")]
        public virtual string Description { get; set; } // vitrual properties allows overriding in the derived classes
    }
}
