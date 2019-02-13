# Changelog

## 0.1.1 (13 Feb 2019)

- Fix error messages for invalid control characters in multi-line strings
- Fix parsing datetime RFC339 with a whitespace instead of T between the day and hour
- Fix parsing of nan/-nan/+nan
- Improve output of TomlFloat ToString for nan/-nan/+nan

## 0.1.0 (12 Feb 2019)

- Initial version (support for TOML 0.5)
