using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherV2.Events
{
    public class ShortcutRegistEvent : PubSubEvent<string>
    {
    }

    public class GeneralRegistEvent : PubSubEvent<string>
    {
    }
}
