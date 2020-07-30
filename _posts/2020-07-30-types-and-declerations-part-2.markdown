---
layout: post
title:  "Types and Declarations Part 2"
date:   2020-07-30 18:00:00 +0100
categories: humphrey language
---

The previous post covered some initial ground on type handling in Humphrey, I`ll repeat the summary here;

A type definition :
```
a32BitIntegerType : [32]bit
```
A variable definition :
```
a32BitIntegerValue : [32]bit = 0
```

Now, for variable definitions, it is not necassary to specifiy the type. This is similar to type inference in other languages;

in 'c#':
```c#
var myVar = 0;
```
in Humphrey :
```
myVar := 0
```

One important point of note however, in the 'c#' example var is an int (32bit) type and value. In Humphrey, constant expressions resolve to a type that is as small as possible to represent the value. In the above case 0 fits into a bit safely, so myVar is a bit type and value. The following additional examples should help to solidify this:

```
var1 := 1
var2 := -1
var3 := 5 + ( 3 * 10 )
var4 := 0 - 7
```

The above are equivelant to :

```
var1 :     bit = 1
var2 : [-2]bit = -1
var3 : [ 6]bit = 35
var4 : [-4]bit = -7
```

Arrays/Pointers and structures are all supported, the syntax for these is as follows;

ArrayType:
```
arrayOfTen_32BitValues : [10][32]bit
```
PointerType:
```
pointerTo32BitValue : *[32]bit
```
StructureType
```
point2D :
{
    x : [-32]bit
    y : [-32]bit
}
```

As you should by now have noticed, if a definition does not have an initial assignment '=' then it is a type definition. A variable must always have an initialiser, at present you can specify the value is initialised with an undefined value '_' and in the future '0' will be allowed as a way to initialise all elements of a value to zero.

e.g.
```
structureValue : { a:bit b:bit } = _
bitValue : bit = _
arrayOfStructuresValue : [10]{ a:bit } = _
```

Next time I'll round out types and declerations with some details on functions.