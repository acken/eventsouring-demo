using System;

namespace Demo
{
    abstract class Event
    {
        public Guid AggregateId { get; set; }
        public int Version { get; set; }
    }
}
