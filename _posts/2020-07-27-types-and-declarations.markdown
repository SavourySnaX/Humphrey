---
layout: post
title:  "Types and Declarations"
date:   2020-07-27 18:00:00 +0100
categories: humphrey language
---

This post will cover some basic ground on how types and declarations work in Humphrey.

Humphrey currently supports signed and unsigned integers of any arbitrary size, although it only has the one built in type `bit`. There is currently no support for floating point types, although it is planned, they are currently unnecassary for the project I`m working on with this language. More on that project at a later date.

If there is only one built in type, how does humphrey support arbitrary sized integers? I`ll handle this question with a couple of examples :

In C you might write the following variable declarations :
```c
unsigned int  myUnsignedInteger = 0;
unsigned char myUnsignedByte    = 0;
short         mySignedShort     = 0;
```
In Humphrey you would write them as follows (sorry no syntax highlighting support for Humphrey on github):
```
myUnsignedInteger : [32]bit  = 0
myUnsignedByte    : [8]bit   = 0
mySignedShort     : [-16]bit = 0
```

There are some obvious differences here, note that types follow the identifiers in Humphrey. Note the definition of mySignedShort, by default types are unsigned, the - here indicates we want a sign bit for the integer. The nice thing about this syntax is I find it reads quite naturally "myUnsignedInteger is a 32bit container holding the value 0". 

If you tend to write code in 'C' using the more specific fixed size types :

```c
uint32_t    myUnsignedInteger = 0;
uint8_t     myUnsignedByte    = 0;
int_16_t    mySignedShort     = 0;
```

Then you are free to define them in Humphrey as follows :

```
uint32_t : [32]bit
uint8_t  : [8]bit
int16_t  : [-16]bit
```

and they can be used as follows :

```
myUnsignedInteger : uint32_t = 0
myUnsignedByte    : uint8_t  = 0
mySignedShort     : int16_t  = 0
```

So to summarise how the syntax works;

A type definition :
```
a32BitIntegerType : [32]bit
```
A variable definition :
```
a32BitIntegerValue : [32]bit = 0
```
