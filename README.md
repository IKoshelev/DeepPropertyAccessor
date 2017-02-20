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
