# Contributing

Thanks for taking the time to consider contributing.

This project is intentionally kept small, explicit, and dependency-light.
Contributions are welcome, but the bar for changes is deliberate by design.

---

## Philosophy

Before contributing, please understand the core principles of this project:

- Explicit is better than implicit
- Architectural clarity is more important than feature count
- Distributed systems are not transparent
- Avoid abstractions that hide real-world trade-offs
- Prefer simple, readable code over clever solutions

If a change violates these principles, it is unlikely to be accepted.

---

## What Contributions Are Welcome

- Bug fixes with clear reproduction steps
- Improvements to documentation and examples
- Architectural discussions and design feedback
- Small, focused enhancements that do not increase conceptual complexity

---

## What Contributions Are Unlikely to Be Accepted

- Large feature additions without prior discussion
- Framework-style abstractions or hidden magic
- Breaking API changes without a strong justification
- Heavy dependencies added for convenience
- Changes that blur the distinction between in-process and distributed dispatch

---

## Code Guidelines

- Keep changes focused and minimal
- Do not introduce breaking changes casually
- Public APIs should be carefully considered
- Avoid reflection-heavy or runtime-magic solutions unless strictly necessary
- Prefer composition over inheritance

---

## Pull Request Guidelines

When opening a pull request:

- Clearly explain **why** the change is needed
- Describe the trade-offs involved
- Reference relevant issues or discussions if applicable
- Keep commits small and logically grouped

Pull requests that only describe *what* changed, but not *why*, may be rejected.

---

## Discussions and Ideas

If you have an idea that significantly changes behavior or scope:

- Open an issue first
- Describe the problem you are trying to solve
- Explain why the existing design is insufficient
- Be prepared to discuss alternatives

---

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
