# GRIFData - Loading and saving GRIF game data files

This is a simple class library to centralize the loading and saving of game data files for GRIF (Game Runner for Interactive Fiction).

The format of a GRIF game data file is a JSON object, but it is non-standard in that `\r`, `\n`, and `\t` are allowed in the string values without being escaped. These are added when saving and removed when loading, for any values which are DAGS scripts. This change allows the data files to be more readable and editable.

The DAGS script values all start with `@`. They are indented in the output using the DAGS routine `PrettyScript()`.

The keys in the output file are sorted both alphabetically and numerically. If a key uses "." to separarate sections and has numeric sections, like `room.23.description`, it would be sorted in this case alphabetically for the first and third sections but numerically for the second. A section of `*`, the generic value, is always placed first.

GRIFData uses DAGS (Data Access Game Scripts) for formatting the game scripts and GROD (Game Resource Overlay Dictionary) for holding in-memory text resources. See the DAGS and GROD GitHub sites for information on those, as well as their NuGet packages.