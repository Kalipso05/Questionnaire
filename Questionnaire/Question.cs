using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Questionnaire
{
    public class Question
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public string QuestionText { get; set; }
    }
}
