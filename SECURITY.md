# Security policy

## Supported versions

Security fixes go into the latest release of the current major version (2.x). Older versions are not patched:
upgrade to the latest release.

## Reporting a vulnerability

Please do not open a public issue. Use GitHub's private reporting: open the repository's **Security** tab and choose
**Report a vulnerability**. Include the provider and its version, the version of Forget, and the smallest code that
reproduces the problem.

## What is in scope

Forget builds SQL, so the reports that matter most are the ones where it generates a statement that does something the
caller did not ask for. The model it is built on:

- values are sent to the database as parameters, not concatenated into the SQL, and the wildcards of `Contains`,
  `StartsWith` and `EndsWith` are escaped;
- a property name given to `FilterDescriptor`, `SortDescriptor` or an aggregate must match a mapped property of the
  entity, otherwise it is rejected with an `ArgumentException`.

A way around either of these is in scope, and so is any other way to make a Forget-generated statement differ from
what the call says.

Out of scope: vulnerabilities in Dapper or in a database driver (report them to those projects), and misuse that
Forget cannot see, such as building a connection string or mapping attributes from untrusted input.
