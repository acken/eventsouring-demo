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

    class ContactAssigen : Event
    {
        public Guid ContactId { get; set; }
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

        public void AssignContact(Guid id) {
            newEvent(new ContactAssigen() { ContactId = id });
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

    class ContactCreated : Event
    {
    }

    class EmailAssigned : Event
    {
        public string Email { get; set; }
    }

    class Contact : AggregateRoot
    {
        public void CreateContact(Guid id, string email) {
            _id = id;
            newEvent(new ContactCreated());
            newEvent(new EmailAssigned() { Email = email });
        }

        public void AssignEmail(string email) {
            newEvent(new EmailAssigned() { Email = email });
        }

        protected override void apply(Event evt) {
        }
    }

    class Identity
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
    }

    class IdentityListHandler : EventHandler
    {
        public List<Identity> _list;

        public IdentityListHandler(List<Identity> list) {
            _list = list;
        }

        public void Handle(Event evt) {
            if (evt.GetType() == typeof(PersonCreated)) {
                _list.Add(new Identity() { Id = evt.AggregateId, Type = "Person"});
            } else if (evt.GetType() == typeof(ContactCreated)) {
                _list.Add(new Identity() { Id = evt.AggregateId, Type = "Contact"});
            }
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
            var identityList = new List<Identity>();
            // Bootstrapping
            var db = new EventStore();
            var eventBus = new EventBus();
            eventBus.Register(new PersonEventHandler());
            eventBus.Register(new IdentityListHandler(identityList));
            _repo = new EventSourceRepository(db, eventBus);

            // Run some commands
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            NewPersonCommand(id1, "Some One");
            Console.WriteLine("");
            NewPersonCommand(id2, "Someone Else");
            Console.WriteLine("");
            WedPeopleCommand(id1, "Some One Else", id2, "Someone One Else");

            // Run contact command
            var contactId = Guid.NewGuid();
            CreateContactWithEmail(contactId, "som@one.com");

            AssignContact(id1, contactId);

            // Print db contents
            Console.WriteLine("");
            db.PrintEvents();

            // Replay list
            var identityList2 = new List<Identity>();
            var handler = new IdentityListHandler(identityList2);
            foreach (var evt in db.GetAll()) {
                handler.Handle(evt);
            }

            foreach (var id in identityList2) {
                Console.WriteLine("Id: {0} is {1}", id.Id, id.Type);
            }
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

        static void CreateContactWithEmail(Guid id, string email) {
            Console.WriteLine("Running wed people command");
            var contact = new Contact();
            contact.CreateContact(id, email);
            _repo.Stage(contact);
            _repo.Flush();
        }

        static void AssignContact(Guid personId, Guid contactId) {
            var person = _repo.Get<Person>(personId);
            var contact = _repo.Get<Contact>(contactId);
            person.AssignContact(contactId);
            _repo.Stage(person);
            _repo.Flush();
        }
	}
}
