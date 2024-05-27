using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Questionnaire
{
    public class Test
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}
