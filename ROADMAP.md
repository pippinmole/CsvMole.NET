# Roadmap

Things deliberately left out of the current rewrite (see `docs/superpowers/specs/2026-11-05-csvmole-rewrite-design.md`).

- **Writing CSV.** For example, `partial void Write(TextWriter writer, IEnumerable<T> rows)`, generated in the same way
  as parsing.
- **UTF-8 input.** Parse `Stream` / `ReadOnlySpan<byte>` directly without decoding to `char`. Only worth adding if
  benchmarks show a meaningful gap compared with the `char` tokenizer.
- **Delimiters longer than one character.** For example, `||`.
- **Optional columns for nullable members.** Allow a header-mapped column to be missing from the file when its member
  is nullable.
