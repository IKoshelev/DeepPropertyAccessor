# DeepPropertyAccessor
Access deeply nested chains of properties in a null-safe manner with optional delegate to handle (i.e. log) interrupted chains. 

```C#
//...
               //Or .DeepGetStruct(...)
   var val = subject.DeepGet(x => x.Prop.Field.Field.Prop,
                            (chainUpToNull, fullChain) => {
                                var fullChainDescr = fullChain.ToChainDescription();
                                var info =    $"Could not get value of {fullChainDescr}, " +
                                              $"because position {chainUpToNull.Count} " +
                                              $" ({chainUpToNull.Last().Name}) contained null.";
                                _logger.LogInfo(info);
                            });
//...
```

If there are no  nulls in chain - ```val``` will get final value;

Otherwise - ```val``` will be null and delegate will be called with information, how far our code got. Delegate is optional. 

# Supported chains 
Currently supports fields, properties, one-dimensional array indexers and custom indexers. Arguments must be constant primitive types or captured variables of such types, as shown in demo.

```C#
//...
    var dictKey = "foo";
   var val = subject.DeepGet(x => x.Arr[10].Dict[dictKey].Field.Prop);
//...
```

The following should also work, though not tested yet.

```C#
//...
    var someVar = "foo";
   var val = subject.DeepGet(x => x.someMethod(5,someVar).Field.Prop);
//...
```

Secondary parameter access inside chain is not supported.

```C#
//...
   // not supported
   var val = subject.DeepGet(x => x.Arr[x.Fiel2.Prop2].Field.Prop);
   
   //use this instead
    var index = subject.DeepGet(x => x.Fiel2.Prop2);
    var val = subject.DeepGet(x => x.Arr[index].Field.Prop);
//...
```
