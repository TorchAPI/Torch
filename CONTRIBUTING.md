# Making a Pull Request
* Fork this repository and make sure your local **master** branch is up to date with the main repository.
* Create a new branch for your addition with an appropriate name, e.g. **add-restart-command**
  * PRs work by submitting the *entire* branch, so this allows you to continue work without locking up your whole repository.
* Commit your changes to that branch, making sure that you **follow the code guidelines below**.
* Submit your branch as a PR to be reviewed.

## Naming Conventions
* Types: **PascalCase**
    * Prefix interfaces with "**I**"
    * Suffix delegates with "**Del**"
* Methods: **PascalCase**
    * Method names should generally use verbs in the infinitive tense, for example `GetValue()` or `OpenFile()`. Callbacks and events should use present continuous (-ing) or past tense depending on the context.
* Non-Private Members: **PascalCase**
* Private Members: **_camelCase**

## Code Design
* **One type per file** with the exception of nested types and delegate declarations.
* **No public fields** except for consts, use properties instead
* **No stateful static types.** These are a pain to clean up, static types should not store any information.
* Use **[dependency injection](https://stackoverflow.com/a/130862)** when possible. Most Torch code uses constructor injection.
* **Events and actions** should be null checked before calling or invoked with the `action?.Invoke()` syntax.

## Documentation
* All types and members not marked **private** or **internal** should have XML documentation using the `/// <summary>` tag.
    * Interface implementations and overridden methods should use the `/// <inheritdoc />` tag unless the summary needs to be changed from the base/interface summary.
