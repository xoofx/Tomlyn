# Changelog

## 0.4.0 (23 Jan 2021)
- Add support for `Toml.ToModel<T>` and `Toml.TryToModel<T>`

## 0.3.1 (14 Jan 2021)
## 0.3.0 (14 Jan 2021)
- Add support for TOML 1.0.0

## 0.2.0 (13 Apr 2020)
- Fix ToString for all nodes involving trivias and comments.

## 0.1.2 (8 Mar 2020)
- Add an easier way to add DateTime(through DateTimeValueSyntax) using the list initialization syntax

## 0.1.1 (13 Feb 2019)

- Fix error messages for invalid control characters in multi-line strings
- Fix parsing datetime RFC339 with a whitespace instead of T between the day and hour
- Fix parsing of nan/-nan/+nan
- Improve output of TomlFloat ToString for nan/-nan/+nan

## 0.1.0 (12 Feb 2019)

- Initial version (support for TOML 0.5)
