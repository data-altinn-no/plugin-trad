using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.Dan.Plugin.Trad.Models
{
    public class Person
    {
        public string ssn { get; set; }
        public TitleType titleType { get; set; }
        public List<Person> isPrincipalFor { get; set; }
        public Person principal { get; set; }
    }
}
