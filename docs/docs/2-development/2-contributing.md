---
title: Contributing
---
# Contributing Guidelines

Thank you for your interest in contributing to QuestNav! This document outlines the process for contributing to the project and the standards we follow.

## Getting Started

:::info
Before contributing, please make sure you've completed the [Development Environment Setup](./1-development-setup.mdx) guide.
:::

## Code of Conduct

:::warning
We are committed to providing a welcoming and inclusive experience for everyone. All contributors are expected to uphold our Code of Conduct:
- Be respectful and inclusive
- Focus on constructive criticism
- Be patient with new contributors
- Help others learn and grow
  :::

## Development Workflow

### 1. Find or Create an Issue

:::tip
Before starting work, check if there's an existing issue for what you want to contribute. If not, create one to discuss your proposed changes.
:::

- Browse the [Issues](https://github.com/QuestNav/QuestNav/issues) section of the repository
- If you don't find an existing issue, create a new one describing the bug or feature
- Wait for feedback from maintainers before proceeding

### 2. Fork and Clone

- Fork the QuestNav repository to your GitHub account
- Clone your fork locally:
  ```
  git clone https://github.com/YOUR-USERNAME/QuestNav.git
  ```
- Add the original repository as a remote to stay updated:
  ```
  git remote add upstream https://github.com/QuestNav/QuestNav.git
  ```

### 3. Create a Branch

:::warning
Always create a new branch for your changes. Never work directly on the main branch.
:::

```
git checkout -b type/description
```

Branch naming convention:
- `feature/` - New features or enhancements
- `bugfix/` - Bug fixes
- `docs/` - Documentation changes
- `refactor/` - Code refactoring without changing functionality

Example: `feature/add-path-visualization`

### 4. Make Your Changes

- Follow the coding standards (see below)
- Keep your changes focused and related to the issue you're addressing
- Add comments to your code when necessary
- Update documentation as needed

### 5. Format your code
:::note
QuestNav uses Csharpier and Spotless to ensure code formatting is universal. Installation instructions are located in the [Development Environment Setup](./1-development-setup.mdx) guide.
:::
Format your C# code prior to submitting your pull request by running the following from the repository root
  ```shell
  csharpier format unity/Assets/QuestNav/
  ```
Format your Java code prior to submitting your pull request by running the following from the `questnav-lib` directory
  ```shell
  ./gradlew spotlessApply
  ```

### 6. Test Your Changes

:::danger
Always test your changes before submitting a pull request!
:::

- Build the project and test on a Quest headset
- Ensure there are no compiler errors or warnings
- Verify that existing functionality still works

### 7. Commit Your Changes

- Make small, focused commits with clear messages
- Use the present tense and imperative mood ("Add feature" not "Added feature")
- Reference the issue number in the commit message

Example:
```
git commit -m "Add path visualization feature (fixes #42)"
```

### 8. Stay Updated

Regularly sync your fork with the upstream repository:

```
git fetch upstream
git rebase upstream/main
```

### 9. Submit a Pull Request

- Push your branch to your fork:
  ```
  git push origin your-branch-name
  ```
- Go to GitHub and create a pull request from your branch to the main repository
- Fill out the pull request template with all required information
- Link the related issue
- Wait for review from maintainers

## Coding Standards

:::note
Consistent code style helps everyone understand and maintain the codebase.
:::

### C# Style Guidelines

- Use camelCase for private fields, methods, and parameters
- Use PascalCase for public classes
- Use meaningful variable and function names
- Keep functions small and focused on a single task
- Comment complex logic or non-obvious code
- Use XML documentation for public APIs
- Profile the code and ensure it runs efficiently

### Unity-Specific Guidelines

- Organize project folders logically (Scripts, Prefabs, Materials, etc.)
- Use dependency injection instead of sterilizable fields in everywhere besides the main file
- Use as little MonoBehaviours as possible
- Set sensible defaults for inspector values
- Keep the scene hierarchy organized and named clearly
- Use appropriate layer and tag settings

## Pull Request Review Process

:::info
All contributions go through code review before being merged.
:::

1. A maintainer will review your code
2. They may request changes or clarification
3. Once approved, your code will be merged
4. After merging, your branch may be deleted

## Documentation

:::warning
Good documentation is crucial for the project's success.
:::

Please update documentation when:
- Adding new features
- Changing existing functionality
- Fixing bugs that affect user behavior
- Adding new dependencies or requirements

## Need Help?

If you need assistance or have questions:
- Comment on the relevant GitHub issue
- Join our Discord server for real-time support

Thank you for contributing to QuestNav and helping FRC teams navigate their robots with precision!