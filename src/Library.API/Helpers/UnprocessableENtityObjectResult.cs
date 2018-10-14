using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    public class UnprocessableEntityObjectResult:ObjectResult
    {   // we want key value pairs, the triggered error and its error message
        public UnprocessableEntityObjectResult(ModelStateDictionary modelState)
            : base(new SerializableError(modelState))//SerializableError defines a serialable container for storing ModelState information as keyvalue pairs
        {
            if(modelState==null)
                throw new ArgumentNullException(nameof(modelState));
            StatusCode = 442; 
        }
    }
}
