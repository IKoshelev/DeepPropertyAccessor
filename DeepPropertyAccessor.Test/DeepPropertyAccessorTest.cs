using System;
using NUnit;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace DeepPropertyAccessor.Test
{
    [TestFixture]
    public class DeepPropertyAccessorTest
    {
        [Test]
        public void ItCanGetDeepFieldsOfValueType()
        {
            var subject = new Subject<Subject<Subject<Subject<int>>>>();

            var val = subject.DeepGetStruct(x => x.Prop.Field.Prop.Field);

            Assert.That(val.GetType() == typeof(int));
            Assert.That(val == 0);
        }

        [Test]
        public void ItCanGetDeepPropertiesOfValueType()
        {
            var subject = new Subject<Subject<Subject<Subject<int>>>>();

            var val = subject.DeepGetStruct(x => x.Prop.Field.Field.Prop);

            Assert.That(val.GetType() == typeof(int));
            Assert.That(val == 0);
        }

        [Test]
        public void ItCanGetDeepFieldsOfReferenceType()
        {
            var subject = new Subject<Subject<Subject<Subject<RefTest>>>>();

            var val = subject.DeepGet(x => x.Prop.Field.Prop.Field);

            Assert.That(val.GetType() == typeof(RefTest));
            Assert.That(val != null);
        }

        [Test]
        public void ItCanGetDeepPropertiesOfReferenceType()
        {
            var subject = new Subject<Subject<Subject<Subject<RefTest>>>>();

            var val = subject.DeepGet(x => x.Prop.Field.Prop.Prop);

            Assert.That(val.GetType() == typeof(RefTest));
            Assert.That(val != null);
        }

        [Test]
        public void DeepGetWillReturnNullAndCallOnNullInChainDelegate()
        {
            var subject = new Subject<Subject<Subject<Subject<RefTest>>>>();

            subject.Prop.Field = null;

            string fullChain = "";
            List<AccessorChainPart> chainUpToNull = null;

            var val = subject
                        .DeepGet(x => x.Prop.Field.Field.Prop,
                            (_chainUpToNull, _fullChain) => {
                                chainUpToNull = _chainUpToNull;
                                fullChain = _fullChain.ToChainDescription();
                            });

            Assert.That(val == null);
            Assert.That(chainUpToNull != null);
            Assert.That(chainUpToNull.Count == 3);
            Assert.That(chainUpToNull.ElementAt(0).Name == "(root)Subject<Subject<Subject<Subject<RefTest>>>>");
            Assert.That(chainUpToNull.ElementAt(0).Value == subject);
            Assert.That(chainUpToNull.ElementAt(1).Name == "Prop");
            Assert.That(chainUpToNull.ElementAt(1).Value == subject.Prop);
            Assert.That(chainUpToNull.ElementAt(2).Name == "Field");
            Assert.That(chainUpToNull.ElementAt(2).Value == null);
            Assert.That(fullChain == "(root).Prop.Field.Field.Prop");
        }

        [Test]
        public void DeepGetStructWillReturnNullAndCallOnNullInChainDelegate()
        {
            var subject = new Subject<Subject<Subject<Subject<int>>>>();

            subject.Prop.Field = null;

            string fullChain = "";
            List<AccessorChainPart> chainUpToNull = null;

            var val = subject
                        .DeepGetStruct(x => x.Prop.Field.Field.Prop,
                            (_chainUpToNull, _fullChain) => {
                                chainUpToNull = _chainUpToNull;
                                fullChain = _fullChain.ToChainDescription();
                            });

            Assert.That(val == null);
            Assert.That(chainUpToNull != null);
            Assert.That(chainUpToNull.Count == 3);
            Assert.That(chainUpToNull.ElementAt(0).Name == "(root)Subject<Subject<Subject<Subject<Int32>>>>");
            Assert.That(chainUpToNull.ElementAt(0).Value == subject);
            Assert.That(chainUpToNull.ElementAt(1).Name == "Prop");
            Assert.That(chainUpToNull.ElementAt(1).Value == subject.Prop);
            Assert.That(chainUpToNull.ElementAt(2).Name == "Field");
            Assert.That(chainUpToNull.ElementAt(2).Value == null);
            Assert.That(fullChain == "(root).Prop.Field.Field.Prop");
        }

        [Test]
        public void DeepGetOnNullInChainDelegateIsOptional()
        {
            var subject = new Subject<Subject<Subject<Subject<RefTest>>>>();

            subject.Prop.Field = null;

            var val = subject
                        .DeepGet(x => x.Prop.Field.Field.Prop);

            Assert.That(val == null);
        }

        [Test]
        public void DeepGetStructOnNullInChainDelegateIsOptional()
        {
            var subject = new Subject<Subject<Subject<Subject<int>>>>();

            subject.Prop.Field = null;

            var val = subject
                        .DeepGetStruct(x => x.Prop.Field.Field.Prop);

            Assert.That(val == null);
        }
    }

    public class Subject<T> where T: new()
    {
        public T Prop { get; set; }
        public T Field { get; set; }

        public Subject ()
        {
            Prop = new T();
            Field = new T();
        }
    }

    public class RefTest
    {

    }
}
