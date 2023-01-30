using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Owners
{
    public class UserRef : Owner
    {
        public UserRef(string id, string name, string description)
        {
            this.id = id;
            this.name = name;
            this.description = description;
        }

        public UserRef buildRef() { return new UserRef(id, name, description); }

        public UserRef() { }
    }
}
