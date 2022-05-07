using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.GreenSpace {
    [AttributeUsage(AttributeTargets.Class)]
    class GreenSpaceAttribute : Attribute {

        public string id { get; private set; }

        public GreenSpaceAttribute(string id) {
            this.id = id;
        }

    }
}
