using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionSolution
{
    internal class Person
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            foreach (var propertyInfo in this.GetType().GetProperties())
            {
                var propertyName = propertyInfo.Name;
                var propertyValue = propertyInfo.GetValue(this);

                stringBuilder.Append($"{propertyName}:{propertyValue}; ");
            }

            return stringBuilder.ToString();
        }
    }
}
