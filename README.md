<p align="center">
  <img src="docs/static/img/branding/QuestNavLogo.svg" alt="QuestNav" width="400"/>
</p>

<p align="center">
  <strong>High-precision robot localization using Meta Quest headsets</strong>
</p>

<p align="center">
  <a href="https://questnav.gg/">Documentation</a> ·
  <a href="https://github.com/QuestNav/QuestNav/releases">Releases</a> ·
  <a href="https://discord.gg/CsRjKfn8Xa">Discord</a> ·
  <a href="https://setup.questnav.gg/">Setup Tool</a>
</p>

---

QuestNav streams Meta Quest headset pose data to an FRC robot over NetworkTables 4 (NT4). The Quest's Visual-Inertial Odometry (VIO) system provides stable, high-frequency position tracking that works in any environment.

## Getting Started

Full documentation is at **[questnav.gg](https://questnav.gg/)**. The [Quick Start](https://questnav.gg/docs/getting-started/quick-start) guide covers everything from unboxing to a working robot integration.

For automated headset configuration, use the [QuestNav Setup Page](https://setup.questnav.gg/).

## Repository Structure

| Directory | Description |
|-----------|-------------|
| `unity/` | Quest headset application (Unity/C#) |
| `questnav-lib/` | Java vendor library for robot code |
| `questnav-web-ui/` | Web interface for headset configuration |
| `docs/` | Documentation site (Docusaurus) |

## Contributing

See the [Contributing Guide](https://questnav.gg/docs/development/contributing) and [Development Setup](https://questnav.gg/docs/development/development-setup) documentation.

## Contributors

<a href="https://github.com/QuestNav/QuestNav/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=QuestNav/QuestNav" />
</a>

## License

This project is licensed under the [MIT License](LICENSE).
