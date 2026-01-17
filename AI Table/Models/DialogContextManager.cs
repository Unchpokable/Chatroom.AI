using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chatroom.AI.Core;

namespace Chatroom.AI.Models
{
    internal class DialogContextManager
    {
        public string SharedSummary { get; set; }
        public List<ContextMessage> SharedHistory { get; set; }
    }
}
