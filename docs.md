---
layout: page
title: Docs
---

# Comments

Comments begin with the '#' character and continue to the end of the current line.

```
# This is a single line comment
```

Block comments begin with the sequential characters "#!" and end at the sequential characters "!#".

```
#!
 This is a multiline comment
!#
```

> Note: nested multiline comments are correctly handled by the parser.

# Integer Constants

Integers can be represented in any base from base 2-36. Humphrey recognises `0x` (hex) and `0b` (binary) style base prefixes, as well as unicode subscript bases and a non unicode subscript `\_`. Additionally integers can have _ as a seperator, this is ignored by the compiler.

The following are all equivalent :

```
12
0xC
0b1100
C\_16
C₁₆
```

And as an example of using underscores :

```
1000000
1_000_000
000F_4240₁₆
```

the above all represent the same value.