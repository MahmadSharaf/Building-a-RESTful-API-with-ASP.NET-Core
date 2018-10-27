using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Model
{
    public class LinkedCollectionResourceWrapperDto<T> : LinkedResourceBaseDto
        where T : LinkedResourceBaseDto //To ensure that the Dto we're returning a list from will also have the links property
    {
        public IEnumerable<T> Value { get; set; }

        public LinkedCollectionResourceWrapperDto(IEnumerable<T> value)
        {
            Value = value;
        }
    }
}
