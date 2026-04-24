using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts
{
    public interface IWidget
    {
        string Name { get; }
        object View { get; }
    }
}