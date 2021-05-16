using DevExpress.Xpo;

namespace UnitTests
{
    public class MasterTestObject : XPBaseObject
    {
        public MasterTestObject(Session session) : base(session)
        {

        }

        private int id;
        [Key(AutoGenerate = true)]
        public int Id
        {
            get { return id; }
            set { SetPropertyValue(nameof(Id), ref id, value); }
        }


        private string name;
        public string Name
        {
            get { return name; }
            set { SetPropertyValue(nameof(Name), ref name, value); }
        }

        [Association]
        public XPCollection<DetailTestObject> Details => GetCollection<DetailTestObject>(nameof(Details));

    }


}
