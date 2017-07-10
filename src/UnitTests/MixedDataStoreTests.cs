using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevExpress.Xpo.DB;
using SenDev.Xaf.ApplicationServerHosting.Web;
using DevExpress.Xpo;
using System.Linq;
using SenDev.Xaf.ApplicationServerHosting;

namespace UnitTests
{
    [TestClass]
    public class MixedDataStoreTests
    {
        [TestMethod]
        public void TestSingleDataStore()
        {
            InMemoryDataStore dataStore = new InMemoryDataStore(AutoCreateOption.DatabaseAndSchema);
            MixedDataStore mixedDataStore = new MixedDataStore(dataStore, DataStoreMode.SchemaUpdate);
            using (var dataLayer = new SimpleDataLayer(mixedDataStore))
            {
                using (var uow = new UnitOfWork(dataLayer))
                {
                    TestObject1 obj1 = new TestObject1(uow);
                    obj1.Name = "Object 1";
                    uow.CommitChanges();

                    var objects = uow.Query<TestObject1>().ToArray();
                    Assert.AreEqual(1, objects.Length);
                    Assert.AreEqual("Object 1", objects[0].Name);
                    var tables = dataStore.GetStorageTablesList(false);
                    Assert.AreEqual("TestObject1", tables.Single());
                }
            }

        }

        [TestMethod]
        public void TestTwoDataStores()
        {
            InMemoryDataStore dataStore1 = new InMemoryDataStore(AutoCreateOption.DatabaseAndSchema);
            InMemoryDataStore dataStore2 = new InMemoryDataStore(AutoCreateOption.DatabaseAndSchema);
            MixedDataStore mixedDataStore = new MixedDataStore(dataStore1, DataStoreMode.SchemaUpdate);
            mixedDataStore.AddDataStore(dataStore2, DataStoreMode.SchemaUpdate, "2_", true);
            using (var dataLayer = new SimpleDataLayer(mixedDataStore))
            {
                using (var uow = new UnitOfWork(dataLayer))
                {
                    TestObject1 obj1 = new TestObject1(uow);
                    obj1.Name = "Object 1";

                    TestObject2 obj2 = new TestObject2(uow);
                    obj2.Name = "Object 2";

                    uow.CommitChanges();

                    var objects = uow.Query<TestObject1>().ToArray();
                    Assert.AreEqual(1, objects.Length);
                    Assert.AreEqual("Object 1", objects[0].Name);
                    var tables = dataStore1.GetStorageTablesList(false);
                    Assert.AreEqual("TestObject1", tables.Single());

                    tables = dataStore2.GetStorageTablesList(false);
                    Assert.AreEqual("TestObject2", tables.Single());
                }
            }

        }
    }

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
