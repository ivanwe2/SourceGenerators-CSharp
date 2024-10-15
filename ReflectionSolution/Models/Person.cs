using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiredBrainCoffee.Generators;

namespace ReflectionSolution.Models
{
    [GenerateToString]
    public partial class Person
    {
        public string? FirstName { get; set; }
        internal string? MiddleName { get; set; } // not included in generation
        public string? LastName { get; set; }
    }

    public partial class Person
    {
        public string? Class { get; set; }
    }
}
