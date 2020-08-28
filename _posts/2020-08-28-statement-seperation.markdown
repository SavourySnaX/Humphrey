---
layout: post
title:  "Statement Seperation"
date:   2020-08-28 10:00:00 +0100
categories: humphrey language
---

Have you ever wondered why C like languages require `;`'s in some places, but not in others, whereas something like Python does not? Well up until yesterday Humphrey did not require seperation between statements (beyond a simple whitespace). However as I was adding support for pre and post increment operators I suddenly realised an issue with this approach. To be clear Python seperates statements by new lines not white space.

Consider the following :

```
a++
*a=2
```

Since Humphrey only used simple whitespace (stictly speaking it simply reads tokens and reacts and never sees whitespace), the above could be parsed as either :

```
a++
*a=2
```

or

```
a++*a=2
```

There are other examples of this ambiguity with statements in a language like Humphrey, so I had to make a choice as to which way to deal with this. Since I am most familiar with C/C++/C# I decided to opt for statement seperation by `;`. At present semi colons are only required and accepted during statement parsing.

Humphrey doesn't bother with `()`'s on `for` or `if` statements, in C this again would be required to allow for disambiguation between the expression and possible following statement e.g.

```c
if (a==2) *a=2;
```

However since Humphrey disallows single statements (you must always have a block), the expressions get naturally seperated by the `{` e.g.

```
if a==2 { *a=2; }
```

Again its worth noting Humphrey doesn't require `;` seperation for statements that end with code blocks like above, and neither do `C` like languages to be fair.
