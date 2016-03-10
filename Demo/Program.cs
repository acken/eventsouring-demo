using System;
using System.Linq;
using System.Collections.Generic;

namespace Demo
{
    enum MaritalStatus
    {
        Single = 0,
        Married = 1,
        Divorced = 2,
        CivilUnion = 3
    }

    // Events
    class PersonCreated : Event
    {
        public string Name { get; set; }
    }

    class PersonChangedName : Event
    {
        public string Name { get; set; }
    }

    class PersonMarried : Event
    {
        public Guid SpouseId { get; set; }
    }

    // Aggregate
    class Person : AggregateRoot
    {
        private string _name;

        public void CreatePerson(Guid id, string name) {
            _id = id;
            newEvent(new PersonCreated() {Name = name});
        }

        public void ChangeName(string name) {
            newEvent(new PersonChangedName() {Name = name});
        }

        public void Marry(Guid id) {
            newEvent(new PersonMarried() {SpouseId = id});
        }

        protected override void apply(Event evt) {
            Console.WriteLine("Applying " + evt.GetType().ToString());
            if (evt.GetType() == typeof(PersonCreated)) {
                _name = ((PersonCreated)evt).Name;
            } else if (evt.GetType() == typeof(PersonChangedName)) {
                _name = ((PersonChangedName)evt).Name;
            }
        }

        public override string ToString() {
            return _name;
        }
    }

    // Event handler
    class PersonEventHandler : EventHandler
    {
        public void Handle(Event evt) {
            Console.WriteLine("Handling " + evt.GetType().ToString());
        }
    }

	class Program
	{
        private static EventSourceRepository _repo;

		static void Main(string[] args) {
            // Bootstrapping
            var db = new EventStore();
            var eventBus = new EventBus();
            eventBus.Register(new PersonEventHandler());
            _repo = new EventSourceRepository(db, eventBus);

            // Run some commands
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            NewPersonCommand(id1, "Some One");
            Console.WriteLine("");
            NewPersonCommand(id2, "Someone Else");
            Console.WriteLine("");
            WedPeopleCommand(id1, "Some One Else", id2, "Someone One Else");

            // Print db contents
            Console.WriteLine("");
            db.PrintEvents();
        }

        static void NewPersonCommand(Guid id, string name) {
            Console.WriteLine("Running new person command");
            var person = new Person();
            person.CreatePerson(id, name);
            _repo.Stage(person);
            _repo.Flush();
        }

        static void WedPeopleCommand(Guid id1, string name1, Guid id2, string name2) {
            Console.WriteLine("Running wed people command");
            var person1 = _repo.Get<Person>(id1);
            var person2 = _repo.Get<Person>(id2);
            person1.ChangeName(name1);
            person1.Marry(person2.AggregateId);
            person2.ChangeName(name2);
            person2.Marry(person1.AggregateId);
            _repo.Stage(person1);
            _repo.Stage(person2);
            _repo.Flush();
        }
	}
}
