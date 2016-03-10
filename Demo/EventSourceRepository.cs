using System;
using System.Linq;
using System.Collections.Generic;

namespace Demo
{
    class EventSourceRepository
    {
        private EventStore _db;
        private EventBus _bus;
        private List<AggregateRoot> _aggregates = new List<AggregateRoot>();
        private List<Event> _stagedEvents = new List<Event>();

        public EventSourceRepository(EventStore db, EventBus bus) {
            _db = db;
            _bus = bus;
        }

        public T Get<T>(Guid id) where T : AggregateRoot, new() {
            Console.WriteLine("Getting " + id.ToString());
            if (_aggregates.Exists(a => a.AggregateId == id)) {
                return (T)_aggregates.First(a => a.AggregateId == id);
            }
            var aggregate = get<T>(id);
            if (aggregate == null) {
                throw new Exception("Aggregate not found");
            }
            _aggregates.Add(aggregate);
            return aggregate;
        }

        public void Stage(AggregateRoot ar) {
            var newEvents = ar.getNewEvents();
            _stagedEvents.AddRange(newEvents);
            ar.ClearNewEvents();
            if (!_aggregates.Exists(a => a.AggregateId == ar.AggregateId)) {
                _aggregates.Add(ar);
            }
        }

        public void Flush() {
            var conflicts = _stagedEvents
                .GroupBy(e => e.AggregateId)
                .Where(g => _aggregates.First(a => a.AggregateId == g.Key).Version != maxVersionFromEvents(_db.Get(g.Key)));
            if (conflicts.Count() > 0) {
                throw new Exception("Could not store due to concurrency errors");
            }
            foreach (var evt in _stagedEvents) {
                Console.WriteLine("Flushing " + evt.GetType().ToString());
            }
            _db.Save(_stagedEvents.ToArray());
            _stagedEvents.ToList().ForEach(e => _bus.Publish(e));
            _stagedEvents.Clear();
            _aggregates.Clear();
        }

        private T get<T>(Guid id) where T : AggregateRoot, new() {
            var events = _db.Get(id);
            if (events.Length == 0) {
                return null;
            }
            var aggregate = new T();
            aggregate.FromEvents(events.ToArray());
            return aggregate;
        }

        private int maxVersionFromEvents(IEnumerable<Event> events) {
            if (events.Count() == 0) {
                return 0;
            }
            return events.Max(e => e.Version);
        }
    }
}
