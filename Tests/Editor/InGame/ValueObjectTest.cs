using NUnit.Framework;
using KahaGameCore.Combat;

namespace KahaGameCore.Tests
{
    public class ValueObjectTest
    {
        [Test]
        public void create()
        {
            ValueObject valueObjectA = new ValueObject("Test", 100);
            ValueObject valueObjectB = new ValueObject("Test", 100);

            Assert.IsTrue(valueObjectA.UID != valueObjectB.UID);
        }

        [Test]
        public void add()
        {
            ValueObject valueObject = new ValueObject("Test", 100);
            valueObject.Add(100);

            Assert.AreEqual(200, valueObject.Value);
        }

        [Test]
        public void add_with_min_max()
        {
            ValueObject valueObject = new ValueObject("Test", 0);
            valueObject.Add(100, 10);
            Assert.AreEqual(10, valueObject.Value);

            valueObject.Add(-100, 10, 0);
            Assert.AreEqual(0, valueObject.Value);
        }

        [Test]
        public void mutiply()
        {
            ValueObject valueObject = new ValueObject("Test", 100);
            valueObject.Mutiply(10f);

            Assert.AreEqual(1000, valueObject.Value);
        }

        [Test]
        public void mutiply_with_max_min()
        {
            ValueObject valueObject = new ValueObject("Test", 100);
            valueObject.Mutiply(10f, 100);
            Assert.AreEqual(100, valueObject.Value);

            valueObject.Mutiply(0.1f, 100, 50);
            Assert.AreEqual(50, valueObject.Value);
        }
    }
}