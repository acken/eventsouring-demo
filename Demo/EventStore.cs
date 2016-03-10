using System;
using System.Linq;
using System.Collections.Generic;

namespace Demo
{
    class EventStore
    {
        private List<Event> _events = new List<Event>();

        public Event[] Get(Guid id) {
            return _events.Where(e => e.AggregateId == id).ToArray();
        }

        public void Save(Event[] events) {
            _events.AddRange(events);
        }

        public Event[] GetAll() {
            return _events.ToArray();
        }

        public void PrintEvents() {
            for (var i = 0; i < _events.Count; i++) {
                Console.WriteLine("{0} - {1} v{2} {3}", i, _events[i].AggregateId, _events[i].Version, _events[i].GetType().ToString());
            }
        }
    }
}
