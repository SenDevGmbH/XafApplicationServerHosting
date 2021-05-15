using DevExpress.Xpo;

namespace UnitTests
{
    public class TestObject1 : XPBaseObject
    {
        public TestObject1(Session session) : base(session)
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
