using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoApp
{
    public struct ProgressUpdateArgs
    {
        public string taskName { get; set; }
        public bool indeterminateTask { get; set; }
        public string progressText { get; set; }
        public TimeSpan timeRemain { get; set; }
        public string currentTask { get; set; }
    }
}
