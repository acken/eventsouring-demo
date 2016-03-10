using System;
using System.Collections.Generic;

namespace Demo
{
    abstract class AggregateRoot
    {
        private List<Event> _newEvents = new List<Event>();

        protected Guid _id = Guid.Empty;
        protected int _version = 0;
        protected abstract void apply(Event evt);

        public Guid AggregateId { get { return _id; } }
        public int Version { get { return _version; } }

        public void FromEvents(Event[] events) {
            foreach(var evt in events) {
                if (_id == Guid.Empty) {
                    _id = evt.AggregateId;
                }
                apply(evt);
                _version = evt.Version;
            }
        }

        public Event[] getNewEvents() {
            return _newEvents.ToArray();
        }

        public void ClearNewEvents() {
            _newEvents.Clear();
        }

        protected void newEvent(Event evt) {
            if (_id == Guid.Empty) {
                throw new Exception("Cannot assign event to aggregate prior to assigning aggregate id to the aggregate");
            }
            evt.AggregateId = _id;
            evt.Version = _version + 1;
            apply(evt);
            _newEvents.Add(evt);
        }
    }
}
