using Library.API.Entities;
using Library.API.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Services
{
    public class PropertyMappingService : IPropertyMappingService
    {//This custom mapping,Dictionaries are used instead of AutoMapper 

        private Dictionary<string, PropertyMappingValue> _authorPropertyMapping =
            new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {   //Mapping from AuthorDto to Author Entity
                //Key:Source                Value:collection of list of the corresponding field/fields
                { "Id"   , new PropertyMappingValue(new List<string>() { "Id"                   }        ) },
                { "Genre", new PropertyMappingValue(new List<string>() { "Genre"                }        ) },
                { "Age"  , new PropertyMappingValue(new List<string>() { "DateOfBirth"          }, true  ) },
                { "Name" , new PropertyMappingValue(new List<string>() { "FirstName","LastName" }        ) },
            };

        // TSource and TDestination can not be resolved. That can be overcome by a marker interface
        //x private IList<PropertyMapping<TSource, TDestination>> propertyMappings;
        private IList<IPropertyMapping> propertyMappings = new List<IPropertyMapping>();



        public PropertyMappingService()
        {//constructor: add the property mapping to the above list as a mapping From the authorDTO to author entity
            propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
        }



        // Method to get a specific mapping
        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {
            // get matching mapping
            var matchingMapping = propertyMappings.OfType<PropertyMapping<TSource, TDestination>> ();

            if (matchingMapping.Count() == 1)
                return matchingMapping.First()._mappingDictionary;

            throw new Exception($"Cannot find exact property mapping instance for <{typeof(TSource)},{typeof(TDestination)}");
        }

        public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            // the string is separated by ",", so we split it.
            var fieldsAfterSplit = fields.Split(',');

            // run through the fields clauses
            foreach (var field in fieldsAfterSplit)
            {
                // trim
                var trimmedField = field.Trim();

                // remove everything after the first " " - if the fields 
                // are coming from an orderBy string, this part must be 
                // ignored
                var indexOfFirstSpace = trimmedField.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ?
                    trimmedField : trimmedField.Remove(indexOfFirstSpace);

                // find the matching property
                if (!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }
            }
            return true;

        }
    }
}
