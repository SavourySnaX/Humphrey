---
layout: post
title:  "Types and Declarations Part 3"
date:   2020-08-14 18:00:00 +0100
categories: humphrey language
---

In the final part of this guide to types and declarations, I will detail how functions currently work. But before that, there is one more type to talk about :

```
anEnum : bit
{
  cow:=0    
  chicken:=1
}
```

This is roughly equivelant to a c#/c enum. However, it really just represents a named collection of constant values of a particular type. This distinction is important, as technically speaking you could define struct values or even collections of functions this way.

Speaking of functions, a function is declared as follows :

```
IDoNothing : ()() =
{

}
```

The above is a function that takes no input paramaters, and returns no output parameters. Input parameters are specified first, with outputs following in the second set of parenthesis.

Humphrey allows for multiple inputs and multiple outputs and because of this all must be named. Here is a simple function that returns the sum of its inputs :

```
Sum : (lhs:[8]bit,rhs:[8]bit)(sum:[8]bit) =
{
    sum = lhs+ rhs
}
```

If you are familiar with C/C# then this might look a little odd, in Humphrey output parameters are assigned, rather than being returned via a keyword. Lets define another version of sum that additionally returns a bit that indicates if the result was 0 or not :

```
Sum : (lhs:[8]bit, rhs:[8]bit)(sum:[8]bit, zero:bit) =
{
    sum = lhs + rhs
    zero = (lhs+rhs) == 0
}
```

Since functions can supply multiple outputs, all functions return an anonymous struct containing their outputs, e.g. to use the above sum function :

```
    c:=Sum(12,15)
```

Here c is an anonymous struct that looks like this : `{sum:[8]bit zero:bit}` so in order to reference the values you would write :

```
    theSum:=c.sum
    isZero:=c.zero
```

Now this is true even when the function only returns a single output, technically an anonymous struct containing a single value is returned. However Humphrey has a rule, whereby it is allowed to unpick the single element from the structure without requiring you to explicitly specify the element name :

```
AFunc : ()(out:[8]bit) =
{
    out = 25
}

OtherFunc : ()() =
{
    value:[8]bit=_
    value = AFunc()
}
```

In this example since value is already declared to be an `[8]bit` value, `AFunc`'s return value is automatically unpacked so that the assignment is legal `{out:[8]bit}` is an anonymous structure containing a single element, and the element matches the variable being assigned to.
