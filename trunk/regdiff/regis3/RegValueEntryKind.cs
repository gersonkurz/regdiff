using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    public enum RegValueEntryKind
    {
        Unknown = -1,
        None = 0,
        SZ = 1,
        ExpandSZ = 2,
        Binary = 3,
        DWord = 4,
        DWordLittleEndian = 4,
        DWordBigEndian = 5,
        Link = 6,
        MultiSZ = 7,
        ResourceList = 8,
        FullResourceDescriptor = 9,
        ResourceRequirementsList = 10,
        QWord = 11,
        QWordLittleEndian = 11,
    }
}
