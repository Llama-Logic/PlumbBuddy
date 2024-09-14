<h1>Contributing</h1>

Thanks you for your interest in contributing to Llama Logic! In this document we'll outline what you need to know about contributing and how to get started.

- [What contributions we accept](#what-contributions-we-accept)
- [Style Guidelines](#style-guidelines)

# What contributions we accept

In short, we’ll entertain anything that makes Llama Logic better in terms of stability, quality, and capability. This may take the shape of:

- Bug fixes
- Feature implementations
- Tests
- Readme and Wiki articles or updates

Before you start opening Pull Requests on the GitHub project, please review the [Style Guidelines](#style-guidelines).

# Style Guidelines

1. Unless stated otherwise, use conventions implied by the default settings of Visual Studio
2. For multi-statement blocks, use [Allman style](https://en.wikipedia.org/wiki/Indentation_style#Allman_style); for single statements, do not use braces
3. Use four spaces of indentation (no tabs)
4. Use camel case for `internal` and `private` fields and use `readonly` where possible; *do not* prefix fields with `_`
5. Do not use `this.` when not necessary
6. Do not specify the default visibility `private`; visibility should be the first modifier (e.g. `public abstract` not `abstract public`)
7. Specify namespace imports at the top of the file, *outside* of namepsace declarations, and sort them alphabetically
8. Do not use more than one empty line at a time
9. Do not include spurious free spaces (consider enabling "View Whitespace (Ctrl+E, S)" if using Visual Studio, to aid detection)
10. Use language keywords instead of BCL types (e.g. `int`, `string`, `float` instead of `Int32`, `String`, `Single`, etc.) for both type references and method calls (e.g. `int.Parse` instead of `Int32.Parse`)
11. Use `nameof(...)` instead of `"..."`
12. Specify fields at the top, below constructor overloads and the finalizer