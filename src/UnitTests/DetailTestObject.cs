using DevExpress.Xpo;

namespace UnitTests
{
    public class DetailTestObject : XPBaseObject
    {
        public DetailTestObject(Session session) : base(session)
        {

        }

        private int id;
        [Key(AutoGenerate = true)]
        public int Id
        {
            get { return id; }
            set { SetPropertyValue(nameof(Id), ref id, value); }
        }



        private MasterTestObject master;
        [Association]
        public MasterTestObject Master
        {
            get => master;
            set => SetPropertyValue(nameof(Master), ref master, value);
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { SetPropertyValue(nameof(Name), ref name, value); }
        }
    }


}
