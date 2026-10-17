# Shapes that do not compile as Nuru options

These routes are not in the probe build. The source generator treats a nested object as a string, an array of objects as a string array, and a dictionary as a string array. The generated assignments do not compile (`string` to `ProbeOperation`, `string[]` to `Dictionary<string, JsonElement>` and to `ProbeOperation[]`).

That is the mapping result for Roslynk's `multi_query` inputs: `operations` (array of objects) and `arguments` (object map) have no `--json-args` shape.
