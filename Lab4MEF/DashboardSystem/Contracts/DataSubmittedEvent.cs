using System;
using System.Collections.Generic;
using System.Text;
using Prism.Events;

namespace Contracts
{
    public class DataSubmittedEvent : PubSubEvent<string> { }
}