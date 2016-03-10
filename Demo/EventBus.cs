using System;
using System.Collections.Generic;

namespace Demo
{
    class EventBus
    {
        private List<EventHandler> _handlers = new List<EventHandler>();

        public void Register(EventHandler handler) {
            _handlers.Add(handler);
        }

        public void Publish(Event evt) {
            _handlers.ForEach(h => h.Handle(evt));
        }
    }
}
