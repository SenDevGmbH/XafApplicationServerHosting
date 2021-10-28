using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevExpress.Xpo.DB;
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

        [TestMethod]
        public void DataStoresWithTableName()
        {
            InMemoryDataStore dataStore1 = new InMemoryDataStore(AutoCreateOption.DatabaseAndSchema);
            InMemoryDataStore dataStore2 = new InMemoryDataStore(AutoCreateOption.DatabaseAndSchema);
            MixedDataStore mixedDataStore = new MixedDataStore(dataStore1, DataStoreMode.SchemaUpdate);
            mixedDataStore.AddDataStore(dataStore2, DataStoreMode.SchemaUpdate, new[] { nameof(MasterTestObject), nameof(DetailTestObject) });
            using (var dataLayer = new SimpleDataLayer(mixedDataStore))
            {
                using (var uow = new UnitOfWork(dataLayer))
                {
                    TestObject1 obj1 = new TestObject1(uow);
                    obj1.Name = "Object 1";

                    TestObject2 obj2 = new TestObject2(uow);
                    obj2.Name = "Object 2";

                    MasterTestObject masterTestObject = new MasterTestObject(uow);
                    masterTestObject.Details.Add(new DetailTestObject(uow));
                    uow.CommitChanges();

                    var objects = uow.Query<TestObject1>().ToArray();
                    Assert.AreEqual(1, objects.Length);
                    Assert.AreEqual("Object 1", objects[0].Name);
                    var tables = dataStore1.GetStorageTablesList(false);
                    Assert.AreEqual(2, tables.Length);
                    Assert.AreEqual("TestObject1", tables[0]);

                    tables = dataStore2.GetStorageTablesList(false);
                    Assert.AreEqual("MasterTestObject", tables[0]);
                    Assert.AreEqual("DetailTestObject", tables[1]);
                }
            }

        }

    }

}
