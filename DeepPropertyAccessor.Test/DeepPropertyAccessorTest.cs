using System;
using NUnit;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        [Test]
        public void DeepGetCanGetArrayIndex()
        {
            var subject = new Subject<char>();

            subject.Arr = new char[] { 'a', 'b', 'c' };

            var val = subject.DeepGetStruct(x => x.Arr[2]);

            Assert.That(val == 'c');

            var capturedPropIndex = 1;
            val = subject.DeepGetStruct(x => x.Arr[capturedPropIndex]);
            Assert.That(val == 'b');
        }

        [Test]
        public void DeepGetCanGetArrayIndexAdv()
        {
            var subject = new Subject<Subject<char>>();

            subject.Arr = new Subject<char>[]
            {
                null,
                new Subject<char>()
                {
                    Arr = new char[] { 'a', 'b', 'c' }
                }
            };
               

            var val = subject.DeepGetStruct(x => x.Arr[1].Arr[2]);

            Assert.That(val == 'c');

            var capturedPropIndex = 1;
            var getterWasCalled = false;
            List<AccessorChainPart> accessorChainPart = null;
            string transformedAccessor = null;
            val = subject.DeepGetStruct(x => x.Arr[0].Arr[capturedPropIndex],(x,y) => {
                getterWasCalled = true;
                accessorChainPart = x;
                transformedAccessor = y.ToString();
            });
            Assert.That(val == null);
            Assert.That(getterWasCalled == true);

            Assert.That(accessorChainPart != null);
            Assert.That(accessorChainPart.Count == 3);
            Assert.That(accessorChainPart.ElementAt(0).Name == "(root)Subject<Subject<Char>>");
            Assert.That(accessorChainPart.ElementAt(0).Value == subject);
            Assert.That(accessorChainPart.ElementAt(1).Name == "Arr");
            Assert.That(accessorChainPart.ElementAt(1).Value == subject.Arr);
            Assert.That(accessorChainPart.ElementAt(2).Name == "[0]");
            Assert.That(accessorChainPart.ElementAt(2).Value == null);

            Assert.That(transformedAccessor, Is.EqualTo("x => x.Arr[0].Arr[1]"));
        }

        [Test]
        public void DeepGetRewritesCaputredVarExpressionsToTheirCurrentValueAsConst()
        {
            var subject = new Subject<Subject<char>>();

            var a = 11;
            var b = 35;

            string transformedAccessor = null;
            var val = subject.DeepGetStruct(x => x.Arr[a].Arr[b], (x, y) => {
                transformedAccessor = y.ToString();
            });

            Assert.That(val == null);
            Assert.That(transformedAccessor, Is.EqualTo("x => x.Arr[11].Arr[35]"));

            var c = "e";
            var d = "f";

            transformedAccessor = null;
            val = subject.DeepGetStruct(x => x.Dict[c].Dict[d], (x, y) => {
                transformedAccessor = y.ToString();
            });

            Assert.That(val == null);
            Assert.That(transformedAccessor, Is.EqualTo("x => x.Dict.get_Item(\"e\").Dict.get_Item(\"f\")"));
        }
     
        [Test]
        public void DeepGetThrowsExpressionParseExceptionWhenCantParseChainAndCachesResult()
        {
            var subject = new Subject<Subject<int>>();

            var wasCalled = false;
            int? val = null;
            Exception ex = null;
            try
            {
                val = subject.DeepGetStruct(x => x.Arr[x.Prop.Prop].Prop, (x, y) =>
                {
                    var c = y.Body as MemberExpression;
                    wasCalled = true;
                });
            }
            catch (Exception exception)
            {
                ex = exception;
            }

            Assert.That(val == null);
            Assert.That(wasCalled, Is.EqualTo(false));
            Assert.That(val, Is.EqualTo(null));
            Assert.That(ex.GetType(), Is.EqualTo(typeof(ExpressionParseException)));
            Assert.That(
                (ex as ExpressionParseException).Message, 
                Is.EqualTo("Only consant value expressions can be used with Index."));
            ex = null;

            try
            {
                val = subject.DeepGetStruct(x => x.Arr[x.Prop.Prop].Prop);
            }
            catch (Exception exception)
            {
                ex = exception;
            }

            Assert.That(val, Is.EqualTo(null));
            Assert.That(ex.GetType(), Is.EqualTo(typeof(ExpressionParseException)));
            Assert.That(
                (ex as ExpressionParseException).Message,
                Is.EqualTo("Expression has been previously deemed invalid."));
            ex = null;

            try
            {
                val = subject.DeepGetStruct(x => x.Prop.Unhandlable(new object()));
            }
            catch (Exception exception)
            {
                ex = exception;
            }

            Assert.That(val, Is.EqualTo(null));
            Assert.That(ex.GetType(), Is.EqualTo(typeof(ExpressionParseException)));
            Assert.That(
                (ex as ExpressionParseException).Message,
                Is.EqualTo("MethodCall can only have constant arguments."));
            ex = null;
        }

        [Test]
        public void DeepGetWrapsArrayAccessorInTryCatchAndNotifiesException()
        {
            var subject = new Subject<Subject<char>>();

            subject.Arr = new Subject<char>[] { };


            var wasCalled = false;
            char? val = null;
            List<AccessorChainPart> accessorChainPart = null;
            val = subject.DeepGetStruct(x => x.Arr[10].Prop, (x, y) =>
            {
                wasCalled = true;
                accessorChainPart = x;
            });

            Assert.That(val == null);
            Assert.That(wasCalled, Is.EqualTo(true));
            Assert.That(val, Is.EqualTo(null));

            Assert.That(accessorChainPart != null);
            Assert.That(accessorChainPart.Count == 3);
            Assert.That(accessorChainPart.ElementAt(0).Name == "(root)Subject<Subject<Char>>");
            Assert.That(accessorChainPart.ElementAt(0).Value == subject);
            Assert.That(accessorChainPart.ElementAt(1).Name == "Arr");
            Assert.That(accessorChainPart.ElementAt(1).Value == subject.Arr);
            Assert.That(accessorChainPart.ElementAt(2).Name == "[10]");
            Assert.That(accessorChainPart.ElementAt(2).Value == null);
            Assert.That(accessorChainPart.ElementAt(2).ExceptionThrown != null);
            Assert.That(accessorChainPart.ElementAt(2).ExceptionThrown.Message == "Index was outside the bounds of the array.");
        }

        [Test]
        public void DeepGetCanGetDictKey()
        {
            var subject = new Subject<char>();

            subject.Dict = new Dictionary<string, char>()
            {
                { "a", 'b'},
                { "c", 'd'},
                { "e", 'f'}
            };
                
            var val = subject.DeepGetStruct(x => x.Dict["e"]);

            Assert.That(val == 'f');

            var capturedPropKey = "c";
            val = subject.DeepGetStruct(x => x.Dict[capturedPropKey]);
            Assert.That(val == 'd');
        }

        [Test]
        public void DeepGetCanGetDictKeyAdv()
        {
            var subject = new Subject<Subject<char>>();

            subject.Dict = new Dictionary<string, Subject<char>>()
            {
                {"a", null },
                {"b", new Subject<char>()
                {
                    Dict = new Dictionary<string, char>()
                    {
                        { "a", 'b'},
                        { "c", 'd'},
                        { "e", 'f'}
                    }
                }}
            };

            var val = subject.DeepGetStruct(x => x.Dict["b"].Dict["c"]);

            Assert.That(val == 'd');

            var capturedPropIndex = "c";
            var getterWasCalled = false;
            List<AccessorChainPart> accessorChainPart = null;
            string transformedAccessor = null;
            val = subject.DeepGetStruct(x => x.Dict["a"].Dict[capturedPropIndex], (x, y) =>
            {
                getterWasCalled = true;
                accessorChainPart = x;
                transformedAccessor = y.ToString();
            });
            Assert.That(val == null);
            Assert.That(getterWasCalled == true);

            Assert.That(accessorChainPart != null);
            Assert.That(accessorChainPart.Count == 3);
            Assert.That(accessorChainPart.ElementAt(0).Name == "(root)Subject<Subject<Char>>");
            Assert.That(accessorChainPart.ElementAt(0).Value == subject);
            Assert.That(accessorChainPart.ElementAt(1).Name == "Dict");
            Assert.That(accessorChainPart.ElementAt(1).Value == subject.Dict);
            Assert.That(accessorChainPart.ElementAt(2).Name == "[\"a\"]");
            Assert.That(accessorChainPart.ElementAt(2).Value == null);

            Assert.That(transformedAccessor, Is.EqualTo("x => x.Dict.get_Item(\"a\").Dict.get_Item(\"c\")"));
        }

        [Test]
        public void DeepGetWrapsMethodCallInTryCatchAndNotifiesException()
        {
            var subject = new Subject<Subject<char>>();

            subject.Dict = new Dictionary<string, Subject<char>>();

            var wasCalled = false;
            char? val = null;
            List<AccessorChainPart> accessorChainPart = null;
            val = subject.DeepGetStruct(x => x.Dict["10"].Prop, (x, y) =>
            {
                wasCalled = true;
                accessorChainPart = x;
            });

            Assert.That(val == null);
            Assert.That(wasCalled, Is.EqualTo(true));
            Assert.That(val, Is.EqualTo(null));

            Assert.That(accessorChainPart != null);
            Assert.That(accessorChainPart.Count == 3);
            Assert.That(accessorChainPart.ElementAt(0).Name == "(root)Subject<Subject<Char>>");
            Assert.That(accessorChainPart.ElementAt(0).Value == subject);
            Assert.That(accessorChainPart.ElementAt(1).Name == "Dict");
            Assert.That(accessorChainPart.ElementAt(1).Value == subject.Dict);
            Assert.That(accessorChainPart.ElementAt(2).Name == "[\"10\"]");
            Assert.That(accessorChainPart.ElementAt(2).Value == null);
            Assert.That(accessorChainPart.ElementAt(2).ExceptionThrown != null);
            Assert.That(accessorChainPart.ElementAt(2).ExceptionThrown.Message == "Exception has been thrown by the target of an invocation.");
        }

        [Test]
        public void DeepGetWrapsMethodCallInTryCatchAndNotifiesException_CustomIndexer()
        {
            var subject = new Subject<Subject<char>>();

            subject.Dict = new Dictionary<string, Subject<char>>();

            var wasCalled = false;
            char? val = null;
            List<AccessorChainPart> accessorChainPart = null;
            val = subject.DeepGetStruct(x => x["10"].Prop, (x, y) =>
            {
                wasCalled = true;
                accessorChainPart = x;
            });

            Assert.That(val == null);
            Assert.That(wasCalled, Is.EqualTo(true));
            Assert.That(val, Is.EqualTo(null));

            Assert.That(accessorChainPart != null);
            Assert.That(accessorChainPart.Count == 2);
            Assert.That(accessorChainPart.ElementAt(0).Name == "(root)Subject<Subject<Char>>");
            Assert.That(accessorChainPart.ElementAt(0).Value == subject);
            Assert.That(accessorChainPart.ElementAt(1).Name == "[\"10\"]");
            Assert.That(accessorChainPart.ElementAt(1).Value == null);
            Assert.That(accessorChainPart.ElementAt(1).ExceptionThrown != null);
            Assert.That(accessorChainPart.ElementAt(1).ExceptionThrown.Message == "Exception has been thrown by the target of an invocation.");
        }
    }

    public class Subject<T> where T: new()
    {
        public T Prop { get; set; }
        public T Field { get; set; }
        public T[] Arr { get; set; }
        public Dictionary<string,T> Dict { get; set; }
        public T Unhandlable()
        {
            throw new NotImplementedException();
        }
        public T Unhandlable(object a)
        {
            throw new NotImplementedException();
        }
        public T Throwing(T a, T b)
        {
            throw new NotImplementedException();
        }

        public T this[string i]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
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
