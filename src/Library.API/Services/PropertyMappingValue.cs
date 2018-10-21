using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Services
{   // Has the destination values
    public class PropertyMappingValue
    {
        public IEnumerable<string> DestinationProperties { get; private set; }//IEnumerable string of destination properties, one resource property will map to
        public bool Revert { get; private set; }//This allow to revert the sort ordering if needed
        public PropertyMappingValue(IEnumerable<string> destinationProperties,
            bool revert = false)
        {
            DestinationProperties = destinationProperties;
            Revert = revert;
        }
    }
}
