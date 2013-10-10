using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3.ini
{
    public class IniFileSection
    {
        public readonly List<IniFileSection> Sections = new List<IniFileSection>();
        public readonly List<IniFileEntry> Entries = new List<IniFileEntry>();
        public string Name { get; protected set; }
        public readonly IniFileSection Parent;

        protected IniFileSection()
        {
            Name = null;
            Parent = null;
        }

        public IniFileSection(string name, IniFileSection parent)
        {
            Name = name;
            Parent = parent;
            Parent.Sections.Add(this);
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder();

            foreach (IniFileSection section in Sections)
            {
                output.AppendLine(section.ToString());
            }

            if (Parent != null)
            {
                Console.WriteLine("[{0}]", Name);
            }

            foreach (IniFileEntry entry in Entries)
            {
                if (entry.HasComment)
                {
                    Console.WriteLine("{0}={1} # {2}",
                        entry.Name, entry.Data, entry.Comment);
                }
                else
                {
                    Console.WriteLine("{0}={1}",
                        entry.Name, entry.Data);
                }
            }
            
            return output.ToString();
        }
    }
}
