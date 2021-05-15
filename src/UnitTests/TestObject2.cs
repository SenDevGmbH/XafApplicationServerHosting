using DevExpress.Xpo;

namespace UnitTests
{
    [Persistent("2_TestObject2")]
    public class TestObject2 : XPBaseObject
    {
        public TestObject2(Session session) : base(session)
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
    }
}
