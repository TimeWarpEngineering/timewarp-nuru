# Roslynator Rules Reference

This document contains all Roslynator analyzer rules for easy discovery and reference. Copy the rules you want to use into your `.editorconfig` file.

## Sources and Documentation

- **Official Documentation**: https://josefpihrt.github.io/docs/roslynator/analyzers
- **GitHub Repository**: https://github.com/dotnet/roslynator
- **Analyzers List**: https://github.com/dotnet/roslynator/blob/main/src/Analyzers/README.md
- **Default Severities**: https://github.com/dotnet/roslynator/blob/main/src/Analyzers/Analyzers.xml
- **Configuration Examples**: https://github.com/dotnet/roslynator/blob/main/docs/configuration.md
- **Changelog**: https://github.com/dotnet/roslynator/blob/main/ChangeLog.md

**Note**: The Roslynator project is actively maintained and new analyzers are added regularly. Check the official sources above for the most up-to-date list of rules and their descriptions.

## How to Use This Document

1. Browse through the rules to understand what's available
2. Copy the rules you want to enforce to your `.editorconfig`
3. Adjust severity levels as needed:
   - `none` - Disabled
   - `silent` - No visible indication
   - `suggestion` - Dots under code
   - `warning` - Squiggly underline
   - `error` - Red squiggly, fails build

## Bulk Configuration

You can configure all Roslynator rules at once:

```ini
# Set default severity for all Roslynator analyzers
dotnet_analyzer_diagnostic.category-roslynator.severity = warning

# Or set all analyzers to a default
dotnet_analyzer_diagnostic.severity = suggestion
```

## Formatting Analyzers (RCS0xxx)

These control code formatting and are disabled by default.

```ini
# RCS0001: Add blank line after embedded statement
dotnet_diagnostic.RCS0001.severity = none

# RCS0002: Add blank line after #region
dotnet_diagnostic.RCS0002.severity = none

# RCS0003: Add blank line after using directive list
dotnet_diagnostic.RCS0003.severity = none

# RCS0004: Add blank line before using directive list
dotnet_diagnostic.RCS0004.severity = none

# RCS0005: Add blank line before closing brace
dotnet_diagnostic.RCS0005.severity = none

# RCS0006: Add blank line before directive
dotnet_diagnostic.RCS0006.severity = none

# RCS0007: Add blank line between accessors
dotnet_diagnostic.RCS0007.severity = none

# RCS0008: Add blank line between closing brace and next statement
dotnet_diagnostic.RCS0008.severity = none

# RCS0009: Add blank line between declaration and documentation comment
dotnet_diagnostic.RCS0009.severity = none

# RCS0010: Add blank line between declarations
dotnet_diagnostic.RCS0010.severity = none

# RCS0011: Add blank line between single-line accessors
dotnet_diagnostic.RCS0011.severity = none

# RCS0012: Add blank line between single-line declarations
dotnet_diagnostic.RCS0012.severity = none

# RCS0013: Add blank line between single-line declarations of different kind
dotnet_diagnostic.RCS0013.severity = none

# RCS0014: Add blank line between switch sections
dotnet_diagnostic.RCS0014.severity = none

# RCS0015: Add blank line between using directives with different root namespace
dotnet_diagnostic.RCS0015.severity = none

# RCS0016: Add new line after attribute list
dotnet_diagnostic.RCS0016.severity = none

# RCS0020: Format accessor's braces on a single line or multiple lines
dotnet_diagnostic.RCS0020.severity = none

# RCS0021: Format block's braces on a single line or multiple lines
dotnet_diagnostic.RCS0021.severity = none

# RCS0022: Format empty block
dotnet_diagnostic.RCS0022.severity = none

# RCS0023: Format type declaration's braces
dotnet_diagnostic.RCS0023.severity = none

# RCS0024: Add new line after switch label
dotnet_diagnostic.RCS0024.severity = none

# RCS0025: Put full accessor on its own line
dotnet_diagnostic.RCS0025.severity = none

# RCS0027: Place new line after/before binary operator
dotnet_diagnostic.RCS0027.severity = none

# RCS0028: Place new line after/before '?:' operator
dotnet_diagnostic.RCS0028.severity = none

# RCS0029: Put constructor initializer on its own line
dotnet_diagnostic.RCS0029.severity = none

# RCS0030: Add new line before embedded statement
dotnet_diagnostic.RCS0030.severity = none

# RCS0031: Add new line before enum member
dotnet_diagnostic.RCS0031.severity = none

# RCS0032: Place new line after/before arrow token
dotnet_diagnostic.RCS0032.severity = none

# RCS0033: Add new line before statement
dotnet_diagnostic.RCS0033.severity = none

# RCS0034: Add new line before type parameter constraint
dotnet_diagnostic.RCS0034.severity = none

# RCS0036: Remove blank line between single-line declarations of same kind
dotnet_diagnostic.RCS0036.severity = none

# RCS0038: Remove blank line between using directives with same root namespace
dotnet_diagnostic.RCS0038.severity = none

# RCS0039: Remove new line before base list
dotnet_diagnostic.RCS0039.severity = none

# RCS0041: Remove new line between 'if' keyword and 'else' keyword
dotnet_diagnostic.RCS0041.severity = none

# RCS0042: Put auto-property on a single line
dotnet_diagnostic.RCS0042.severity = none

# RCS0043: Format accessor's braces on a single line when expression is on single line
dotnet_diagnostic.RCS0043.severity = none

# RCS0044: Use carriage return + linefeed as new line
dotnet_diagnostic.RCS0044.severity = none

# RCS0045: Use linefeed as new line
dotnet_diagnostic.RCS0045.severity = none

# RCS0046: Use spaces instead of tab
dotnet_diagnostic.RCS0046.severity = none

# RCS0048: Put initializer on a single line
dotnet_diagnostic.RCS0048.severity = none

# RCS0049: Add blank line after top comment
dotnet_diagnostic.RCS0049.severity = none

# RCS0050: Add blank line before top declaration
dotnet_diagnostic.RCS0050.severity = none

# RCS0051: Add/remove new line before 'while' in 'do' statement
dotnet_diagnostic.RCS0051.severity = none

# RCS0052: Place new line after/before equals token
dotnet_diagnostic.RCS0052.severity = none

# RCS0053: Fix formatting of a list
dotnet_diagnostic.RCS0053.severity = none

# RCS0054: Fix formatting of a call chain
dotnet_diagnostic.RCS0054.severity = none

# RCS0055: Fix formatting of a binary expression chain
dotnet_diagnostic.RCS0055.severity = none

# RCS0056: A line is too long
dotnet_diagnostic.RCS0056.severity = none

# RCS0057: Normalize whitespace at the beginning of a file
dotnet_diagnostic.RCS0057.severity = none

# RCS0058: Normalize whitespace at the end of a file
dotnet_diagnostic.RCS0058.severity = none

# RCS0059: Place new line after/before null-conditional operator
dotnet_diagnostic.RCS0059.severity = none

# RCS0060: Add blank line after file scoped namespace declaration (Experimental)
dotnet_diagnostic.RCS0060.severity = none

# RCS0061: Add blank line between switch sections (Experimental)
dotnet_diagnostic.RCS0061.severity = none
```

## General Analyzers (RCS1xxx)

These are the main code quality analyzers.

```ini
# RCS1001: Add braces (when expression spans over multiple lines)
dotnet_diagnostic.RCS1001.severity = warning

# RCS1002: Remove braces
dotnet_diagnostic.RCS1002.severity = none

# RCS1003: Add braces to if-else (when expression spans over multiple lines)
dotnet_diagnostic.RCS1003.severity = warning

# RCS1004: Remove braces from if-else
dotnet_diagnostic.RCS1004.severity = none

# RCS1005: Simplify nested using statement
dotnet_diagnostic.RCS1005.severity = suggestion

# RCS1006: Merge 'else' with nested 'if'
dotnet_diagnostic.RCS1006.severity = suggestion

# RCS1007: Add braces
dotnet_diagnostic.RCS1007.severity = none

# RCS1008: Use predefined type
dotnet_diagnostic.RCS1008.severity = none

# RCS1009: Use predefined type
dotnet_diagnostic.RCS1009.severity = warning

# RCS1010: Use 'var' instead of explicit type (when the type is obvious)
dotnet_diagnostic.RCS1010.severity = none

# RCS1012: Use predefined type
dotnet_diagnostic.RCS1012.severity = warning

# RCS1013: Use predefined type
dotnet_diagnostic.RCS1013.severity = none

# RCS1014: Use explicitly typed array (or implicitly typed array)
dotnet_diagnostic.RCS1014.severity = none

# RCS1015: Use nameof operator
dotnet_diagnostic.RCS1015.severity = warning

# RCS1016: Use block body or expression body
dotnet_diagnostic.RCS1016.severity = none
# Required: roslynator_body_style = block|expression|when_on_single_line

# RCS1017: Avoid multiline expression body
dotnet_diagnostic.RCS1017.severity = none

# RCS1018: Add accessibility modifiers (or remove accessibility modifiers)
dotnet_diagnostic.RCS1018.severity = warning

# RCS1019: Order modifiers
dotnet_diagnostic.RCS1019.severity = warning

# RCS1020: Simplify Nullable<T> to T?
dotnet_diagnostic.RCS1020.severity = warning

# RCS1021: Convert lambda expression body to expression body
dotnet_diagnostic.RCS1021.severity = none

# RCS1031: Remove unnecessary braces
dotnet_diagnostic.RCS1031.severity = suggestion

# RCS1032: Remove redundant parentheses
dotnet_diagnostic.RCS1032.severity = suggestion

# RCS1033: Remove redundant boolean literal
dotnet_diagnostic.RCS1033.severity = warning

# RCS1034: Remove redundant 'sealed' modifier
dotnet_diagnostic.RCS1034.severity = suggestion

# RCS1035: Remove redundant comma in initializer
dotnet_diagnostic.RCS1035.severity = suggestion

# RCS1036: Remove unnecessary blank line
dotnet_diagnostic.RCS1036.severity = none

# RCS1037: Remove trailing white-space
dotnet_diagnostic.RCS1037.severity = warning

# RCS1038: Remove empty statement
dotnet_diagnostic.RCS1038.severity = warning

# RCS1039: Remove argument list from attribute
dotnet_diagnostic.RCS1039.severity = suggestion

# RCS1040: Remove empty 'else' clause
dotnet_diagnostic.RCS1040.severity = suggestion

# RCS1041: Remove empty initializer
dotnet_diagnostic.RCS1041.severity = suggestion

# RCS1042: Remove enum default underlying type
dotnet_diagnostic.RCS1042.severity = suggestion

# RCS1043: Remove 'partial' modifier from type with a single part
dotnet_diagnostic.RCS1043.severity = warning

# RCS1044: Remove original exception from throw statement
dotnet_diagnostic.RCS1044.severity = warning

# RCS1045: Rename private field to camel case with underscore
dotnet_diagnostic.RCS1045.severity = none

# RCS1046: Asynchronous method name should end with 'Async'
dotnet_diagnostic.RCS1046.severity = none

# RCS1047: Non-asynchronous method name should not end with 'Async'
dotnet_diagnostic.RCS1047.severity = warning

# RCS1048: Use lambda expression instead of anonymous method
dotnet_diagnostic.RCS1048.severity = suggestion

# RCS1049: Simplify boolean comparison
dotnet_diagnostic.RCS1049.severity = suggestion

# RCS1050: Add argument list to object creation expression (or remove parentheses from argument list)
dotnet_diagnostic.RCS1050.severity = none

# RCS1051: Add/remove parentheses from condition of conditional expression
dotnet_diagnostic.RCS1051.severity = none

# RCS1052: Declare each attribute separately
dotnet_diagnostic.RCS1052.severity = none

# RCS1054: Merge local declaration with return statement
dotnet_diagnostic.RCS1054.severity = none

# RCS1055: Avoid semicolon at the end of declaration
dotnet_diagnostic.RCS1055.severity = warning

# RCS1056: Avoid usage of using alias directive
dotnet_diagnostic.RCS1056.severity = none

# RCS1057: Add empty line between declarations
dotnet_diagnostic.RCS1057.severity = none

# RCS1058: Use compound assignment
dotnet_diagnostic.RCS1058.severity = suggestion

# RCS1059: Avoid locking on publicly accessible instance
dotnet_diagnostic.RCS1059.severity = warning

# RCS1060: Declare each type in separate file
dotnet_diagnostic.RCS1060.severity = none

# RCS1061: Merge 'if' with nested 'if'
dotnet_diagnostic.RCS1061.severity = suggestion

# RCS1062: Use simple assert method
dotnet_diagnostic.RCS1062.severity = none

# RCS1063: Avoid usage of do statement to create an infinite loop
dotnet_diagnostic.RCS1063.severity = suggestion

# RCS1064: Avoid usage of for statement to create an infinite loop
dotnet_diagnostic.RCS1064.severity = suggestion

# RCS1065: Avoid usage of while statement to create an infinite loop
dotnet_diagnostic.RCS1065.severity = none

# RCS1066: Remove empty 'finally' clause
dotnet_diagnostic.RCS1066.severity = suggestion

# RCS1067: Remove argument list from object creation expression
dotnet_diagnostic.RCS1067.severity = none

# RCS1068: Simplify logical negation
dotnet_diagnostic.RCS1068.severity = suggestion

# RCS1069: Remove unnecessary case label
dotnet_diagnostic.RCS1069.severity = suggestion

# RCS1070: Remove redundant default switch section
dotnet_diagnostic.RCS1070.severity = suggestion

# RCS1071: Remove redundant base constructor call
dotnet_diagnostic.RCS1071.severity = suggestion

# RCS1072: Remove empty namespace declaration
dotnet_diagnostic.RCS1072.severity = warning

# RCS1073: Convert 'if' to 'return' statement
dotnet_diagnostic.RCS1073.severity = none

# RCS1074: Remove redundant constructor
dotnet_diagnostic.RCS1074.severity = suggestion

# RCS1075: Avoid empty catch clause that catches System.Exception
dotnet_diagnostic.RCS1075.severity = warning

# RCS1077: Optimize LINQ method call
dotnet_diagnostic.RCS1077.severity = suggestion

# RCS1078: Use "" or string.Empty
dotnet_diagnostic.RCS1078.severity = none

# RCS1079: Throwing of new NotImplementedException
dotnet_diagnostic.RCS1079.severity = warning

# RCS1080: Use 'Count/Length' property instead of 'Any' method
dotnet_diagnostic.RCS1080.severity = suggestion

# RCS1081: Split variable declaration
dotnet_diagnostic.RCS1081.severity = none

# RCS1082: Use 'Count/Length' property instead of 'Count' method
dotnet_diagnostic.RCS1082.severity = warning

# RCS1083: Call 'Enumerable.Any' instead of 'Enumerable.Count'
dotnet_diagnostic.RCS1083.severity = warning

# RCS1084: Use coalesce expression instead of conditional expression
dotnet_diagnostic.RCS1084.severity = suggestion

# RCS1085: Use auto-implemented property
dotnet_diagnostic.RCS1085.severity = suggestion

# RCS1086: Use linefeed as new line
dotnet_diagnostic.RCS1086.severity = none

# RCS1087: Use carriage return + linefeed as new line
dotnet_diagnostic.RCS1087.severity = none

# RCS1088: Use spaces instead of tab
dotnet_diagnostic.RCS1088.severity = none

# RCS1089: Use --/++ operator instead of assignment
dotnet_diagnostic.RCS1089.severity = suggestion

# RCS1090: Add/remove 'ConfigureAwait(false)' call
dotnet_diagnostic.RCS1090.severity = warning

# RCS1091: Remove empty region
dotnet_diagnostic.RCS1091.severity = suggestion

# RCS1092: Add empty line after last statement in do statement
dotnet_diagnostic.RCS1092.severity = none

# RCS1093: Remove file with no code
dotnet_diagnostic.RCS1093.severity = warning

# RCS1094: Declare using directive on top level
dotnet_diagnostic.RCS1094.severity = none

# RCS1095: Use C# 6.0 dictionary initializer
dotnet_diagnostic.RCS1095.severity = suggestion

# RCS1096: Convert 'HasFlag' call to bitwise operation (or vice versa)
dotnet_diagnostic.RCS1096.severity = none
# Required: roslynator_enum_has_flag_style = method|operator

# RCS1097: Remove redundant 'ToString' call
dotnet_diagnostic.RCS1097.severity = warning

# RCS1098: Constant values should be placed on right side of comparisons
dotnet_diagnostic.RCS1098.severity = suggestion

# RCS1099: Default label should be the last label in a switch section
dotnet_diagnostic.RCS1099.severity = warning

# RCS1100: Format documentation summary on a single line (or multiple lines)
dotnet_diagnostic.RCS1100.severity = none

# RCS1101: Format documentation summary on multiple lines
dotnet_diagnostic.RCS1101.severity = none

# RCS1102: Make class static
dotnet_diagnostic.RCS1102.severity = warning

# RCS1103: Convert 'if' to assignment
dotnet_diagnostic.RCS1103.severity = suggestion

# RCS1104: Simplify conditional expression
dotnet_diagnostic.RCS1104.severity = suggestion

# RCS1105: Unnecessary interpolation
dotnet_diagnostic.RCS1105.severity = suggestion

# RCS1106: Remove empty destructor
dotnet_diagnostic.RCS1106.severity = warning

# RCS1107: Remove redundant 'StringComparison' argument
dotnet_diagnostic.RCS1107.severity = suggestion

# RCS1108: Add 'static' modifier to all partial class declarations
dotnet_diagnostic.RCS1108.severity = warning

# RCS1109: Call 'Enumerable.Cast' instead of 'Enumerable.Select'
dotnet_diagnostic.RCS1109.severity = suggestion

# RCS1110: Declare type inside namespace
dotnet_diagnostic.RCS1110.severity = warning

# RCS1111: Add braces to switch section with multiple statements
dotnet_diagnostic.RCS1111.severity = none

# RCS1112: Combine 'Enumerable.Where' method chain
dotnet_diagnostic.RCS1112.severity = suggestion

# RCS1113: Use 'string.IsNullOrEmpty' method
dotnet_diagnostic.RCS1113.severity = suggestion

# RCS1114: Remove redundant delegate creation
dotnet_diagnostic.RCS1114.severity = suggestion

# RCS1115: Replace yield return statement with expression statement
dotnet_diagnostic.RCS1115.severity = none

# RCS1116: Add break statement to switch section
dotnet_diagnostic.RCS1116.severity = none

# RCS1117: Add return statement that returns default value
dotnet_diagnostic.RCS1117.severity = none

# RCS1118: Mark local variable as const
dotnet_diagnostic.RCS1118.severity = suggestion

# RCS1119: Call 'Enumerable.Skip' and 'Enumerable.Take' instead of 'Enumerable.GetRange'
dotnet_diagnostic.RCS1119.severity = none

# RCS1120: Use [] instead of calling 'Enumerable.ElementAt'
dotnet_diagnostic.RCS1120.severity = warning

# RCS1121: Use [] instead of calling 'Enumerable.First'
dotnet_diagnostic.RCS1121.severity = warning

# RCS1122: Add missing semicolon
dotnet_diagnostic.RCS1122.severity = warning

# RCS1123: Add parentheses when necessary
dotnet_diagnostic.RCS1123.severity = suggestion

# RCS1124: Inline local variable
dotnet_diagnostic.RCS1124.severity = none

# RCS1125: Mark member as static
dotnet_diagnostic.RCS1125.severity = warning

# RCS1126: Add braces to if-else
dotnet_diagnostic.RCS1126.severity = none

# RCS1127: Merge local declaration with assignment
dotnet_diagnostic.RCS1127.severity = suggestion

# RCS1128: Use coalesce expression
dotnet_diagnostic.RCS1128.severity = suggestion

# RCS1129: Remove redundant field initialization
dotnet_diagnostic.RCS1129.severity = suggestion

# RCS1130: Bitwise operation on enum without Flags attribute
dotnet_diagnostic.RCS1130.severity = warning

# RCS1131: Replace return with yield return
dotnet_diagnostic.RCS1131.severity = none

# RCS1132: Remove redundant overriding member
dotnet_diagnostic.RCS1132.severity = suggestion

# RCS1133: Remove redundant Dispose/Close call
dotnet_diagnostic.RCS1133.severity = suggestion

# RCS1134: Remove redundant statement
dotnet_diagnostic.RCS1134.severity = suggestion

# RCS1135: Declare enum member with zero value (when enum has FlagsAttribute)
dotnet_diagnostic.RCS1135.severity = warning

# RCS1136: Merge switch sections with equivalent content
dotnet_diagnostic.RCS1136.severity = suggestion

# RCS1137: Add documentation comment to publicly visible type or member
dotnet_diagnostic.RCS1137.severity = none

# RCS1138: Add summary to documentation comment
dotnet_diagnostic.RCS1138.severity = none

# RCS1139: Add summary element to documentation comment
dotnet_diagnostic.RCS1139.severity = none

# RCS1140: Add exception to documentation comment
dotnet_diagnostic.RCS1140.severity = none

# RCS1141: Add 'param' element to documentation comment
dotnet_diagnostic.RCS1141.severity = none

# RCS1142: Add 'typeparam' element to documentation comment
dotnet_diagnostic.RCS1142.severity = none

# RCS1143: Simplify coalesce expression
dotnet_diagnostic.RCS1143.severity = none

# RCS1144: Mark containing class as abstract
dotnet_diagnostic.RCS1144.severity = warning

# RCS1145: Remove redundant 'as' operator
dotnet_diagnostic.RCS1145.severity = suggestion

# RCS1146: Use conditional access
dotnet_diagnostic.RCS1146.severity = suggestion

# RCS1147: Remove inapplicable modifier
dotnet_diagnostic.RCS1147.severity = warning

# RCS1148: Remove unreachable code
dotnet_diagnostic.RCS1148.severity = warning

# RCS1149: Remove implementation from abstract member
dotnet_diagnostic.RCS1149.severity = warning

# RCS1150: Call string.Concat instead of string.Join
dotnet_diagnostic.RCS1150.severity = suggestion

# RCS1151: Remove redundant cast
dotnet_diagnostic.RCS1151.severity = suggestion

# RCS1152: Member type can be declared in base type
dotnet_diagnostic.RCS1152.severity = none

# RCS1153: Add empty line after closing brace
dotnet_diagnostic.RCS1153.severity = none

# RCS1154: Sort enum members
dotnet_diagnostic.RCS1154.severity = none

# RCS1155: Use StringComparison when comparing strings
dotnet_diagnostic.RCS1155.severity = warning

# RCS1156: Use string.Length instead of comparison with empty string
dotnet_diagnostic.RCS1156.severity = suggestion

# RCS1157: Composite enum value contains undefined flag
dotnet_diagnostic.RCS1157.severity = warning

# RCS1158: Static member in generic type should use a type parameter
dotnet_diagnostic.RCS1158.severity = none

# RCS1159: Use EventHandler<T>
dotnet_diagnostic.RCS1159.severity = suggestion

# RCS1160: Abstract type should not have public constructors
dotnet_diagnostic.RCS1160.severity = warning

# RCS1161: Enum should declare explicit values
dotnet_diagnostic.RCS1161.severity = none

# RCS1162: Avoid chain of assignments
dotnet_diagnostic.RCS1162.severity = none

# RCS1163: Unused parameter
dotnet_diagnostic.RCS1163.severity = suggestion

# RCS1164: Unused type parameter
dotnet_diagnostic.RCS1164.severity = warning

# RCS1165: Unconstrained type parameter checked for null
dotnet_diagnostic.RCS1165.severity = suggestion

# RCS1166: Value type object is never equal to null
dotnet_diagnostic.RCS1166.severity = warning

# RCS1167: Override of Object.Equals(object) should be overridden
dotnet_diagnostic.RCS1167.severity = none

# RCS1168: Parameter name differs from base name
dotnet_diagnostic.RCS1168.severity = suggestion

# RCS1169: Make field read-only
dotnet_diagnostic.RCS1169.severity = suggestion

# RCS1170: Use read-only auto-implemented property
dotnet_diagnostic.RCS1170.severity = suggestion

# RCS1171: Simplify lazy initialization
dotnet_diagnostic.RCS1171.severity = suggestion

# RCS1172: Use 'is' operator instead of 'as' operator
dotnet_diagnostic.RCS1172.severity = warning

# RCS1173: Use coalesce expression instead of 'if'
dotnet_diagnostic.RCS1173.severity = suggestion

# RCS1174: Remove redundant async/await
dotnet_diagnostic.RCS1174.severity = none

# RCS1175: Unused 'this' parameter
dotnet_diagnostic.RCS1175.severity = suggestion

# RCS1176: Use 'var' instead of explicit type (when the type is not obvious)
dotnet_diagnostic.RCS1176.severity = none

# RCS1177: Use 'var' instead of explicit type (in foreach)
dotnet_diagnostic.RCS1177.severity = none

# RCS1178: Call Debug.Fail instead of Debug.Assert
dotnet_diagnostic.RCS1178.severity = suggestion

# RCS1179: Unnecessary assignment
dotnet_diagnostic.RCS1179.severity = suggestion

# RCS1180: Inline lazy initialization
dotnet_diagnostic.RCS1180.severity = suggestion

# RCS1181: Convert comment to documentation comment
dotnet_diagnostic.RCS1181.severity = none

# RCS1182: Remove redundant base interface
dotnet_diagnostic.RCS1182.severity = suggestion

# RCS1183: Format initializer with single expression on single line
dotnet_diagnostic.RCS1183.severity = none

# RCS1184: Format conditional expression (format ? and : on next line)
dotnet_diagnostic.RCS1184.severity = none

# RCS1185: Format single-line block
dotnet_diagnostic.RCS1185.severity = none

# RCS1186: Use Regex instance instead of static method
dotnet_diagnostic.RCS1186.severity = none

# RCS1187: Mark constant with ConstantAttribute
dotnet_diagnostic.RCS1187.severity = none

# RCS1188: Remove redundant auto-property initialization
dotnet_diagnostic.RCS1188.severity = suggestion

# RCS1189: Add or remove region name
dotnet_diagnostic.RCS1189.severity = none

# RCS1190: Join string expressions
dotnet_diagnostic.RCS1190.severity = suggestion

# RCS1191: Declare enum value as combination of names
dotnet_diagnostic.RCS1191.severity = suggestion

# RCS1192: Unnecessary usage of verbatim string literal
dotnet_diagnostic.RCS1192.severity = suggestion

# RCS1193: Overriding member should not change 'params' modifier
dotnet_diagnostic.RCS1193.severity = warning

# RCS1194: Implement exception constructors
dotnet_diagnostic.RCS1194.severity = warning

# RCS1195: Use ^ operator
dotnet_diagnostic.RCS1195.severity = suggestion

# RCS1196: Call extension method as instance method
dotnet_diagnostic.RCS1196.severity = suggestion

# RCS1197: Optimize StringBuilder.Append/AppendLine call
dotnet_diagnostic.RCS1197.severity = suggestion

# RCS1198: Avoid unnecessary boxing of value type
dotnet_diagnostic.RCS1198.severity = none

# RCS1199: Unnecessary null check
dotnet_diagnostic.RCS1199.severity = suggestion

# RCS1200: Call 'Enumerable.ThenBy' instead of 'Enumerable.OrderBy'
dotnet_diagnostic.RCS1200.severity = suggestion

# RCS1201: Use method chaining
dotnet_diagnostic.RCS1201.severity = none

# RCS1202: Avoid NullReferenceException
dotnet_diagnostic.RCS1202.severity = none

# RCS1203: Use AttributeUsageAttribute
dotnet_diagnostic.RCS1203.severity = warning

# RCS1204: Use EventArgs.Empty
dotnet_diagnostic.RCS1204.severity = suggestion

# RCS1205: Order named arguments according to the order of parameters
dotnet_diagnostic.RCS1205.severity = suggestion

# RCS1206: Use conditional access instead of conditional expression
dotnet_diagnostic.RCS1206.severity = suggestion

# RCS1207: Convert anonymous function to method group (or vice versa)
dotnet_diagnostic.RCS1207.severity = none

# RCS1208: Reduce 'if' nesting
dotnet_diagnostic.RCS1208.severity = none

# RCS1209: Order type parameter constraints
dotnet_diagnostic.RCS1209.severity = suggestion

# RCS1210: Return completed task instead of returning null
dotnet_diagnostic.RCS1210.severity = warning

# RCS1211: Remove unnecessary 'else'
dotnet_diagnostic.RCS1211.severity = suggestion

# RCS1212: Remove redundant assignment
dotnet_diagnostic.RCS1212.severity = suggestion

# RCS1213: Remove unused member declaration
dotnet_diagnostic.RCS1213.severity = suggestion

# RCS1214: Unnecessary interpolated string
dotnet_diagnostic.RCS1214.severity = suggestion

# RCS1215: Expression is always equal to true/false
dotnet_diagnostic.RCS1215.severity = warning

# RCS1216: Unnecessary unsafe context
dotnet_diagnostic.RCS1216.severity = suggestion

# RCS1217: Convert interpolated string to concatenation
dotnet_diagnostic.RCS1217.severity = none

# RCS1218: Simplify code branching
dotnet_diagnostic.RCS1218.severity = suggestion

# RCS1219: Call 'Enumerable.Skip' and 'Enumerable.Any' instead of 'Enumerable.Count'
dotnet_diagnostic.RCS1219.severity = none

# RCS1220: Use pattern matching instead of combination of 'is' operator and cast operator
dotnet_diagnostic.RCS1220.severity = suggestion

# RCS1221: Use pattern matching instead of combination of 'as' operator and null check
dotnet_diagnostic.RCS1221.severity = suggestion

# RCS1222: Merge preprocessor directives
dotnet_diagnostic.RCS1222.severity = suggestion

# RCS1223: Mark publicly visible type with DebuggerDisplay attribute
dotnet_diagnostic.RCS1223.severity = none

# RCS1224: Make method an extension method
dotnet_diagnostic.RCS1224.severity = none

# RCS1225: Make class sealed
dotnet_diagnostic.RCS1225.severity = none

# RCS1226: Add paragraph to documentation comment
dotnet_diagnostic.RCS1226.severity = none

# RCS1227: Validate arguments correctly
dotnet_diagnostic.RCS1227.severity = warning

# RCS1228: Unused element in a documentation comment
dotnet_diagnostic.RCS1228.severity = suggestion

# RCS1229: Use async/await when necessary
dotnet_diagnostic.RCS1229.severity = none

# RCS1230: Unnecessary explicit use of enumerator
dotnet_diagnostic.RCS1230.severity = suggestion

# RCS1231: Make parameter ref read-only
dotnet_diagnostic.RCS1231.severity = none

# RCS1232: Order elements in documentation comment
dotnet_diagnostic.RCS1232.severity = suggestion

# RCS1233: Use short-circuiting operator
dotnet_diagnostic.RCS1233.severity = warning

# RCS1234: Duplicate enum value
dotnet_diagnostic.RCS1234.severity = warning

# RCS1235: Optimize method call
dotnet_diagnostic.RCS1235.severity = suggestion

# RCS1236: Use exception filter
dotnet_diagnostic.RCS1236.severity = none

# RCS1237: Use bit shift operator
dotnet_diagnostic.RCS1237.severity = none

# RCS1238: Avoid nested ?: operators
dotnet_diagnostic.RCS1238.severity = none

# RCS1239: Use 'for' statement instead of 'while' statement
dotnet_diagnostic.RCS1239.severity = suggestion

# RCS1240: Operator is unnecessary
dotnet_diagnostic.RCS1240.severity = suggestion

# RCS1241: Implement non-generic counterpart
dotnet_diagnostic.RCS1241.severity = none

# RCS1242: Do not pass non-read-only struct by read-only reference
dotnet_diagnostic.RCS1242.severity = warning

# RCS1243: Duplicate word in a comment
dotnet_diagnostic.RCS1243.severity = suggestion

# RCS1244: Simplify 'default' expression
dotnet_diagnostic.RCS1244.severity = none

# RCS1245: Simplify conditional expression
dotnet_diagnostic.RCS1245.severity = none

# RCS1246: Use element access
dotnet_diagnostic.RCS1246.severity = suggestion

# RCS1247: Fix documentation comment tag
dotnet_diagnostic.RCS1247.severity = suggestion

# RCS1248: Use pattern matching to check for null (or vice versa)
dotnet_diagnostic.RCS1248.severity = warning
# Required: roslynator_null_check_style = pattern_matching|equality_operator

# RCS1249: Unnecessary null-forgiving operator
dotnet_diagnostic.RCS1249.severity = suggestion

# RCS1250: Use implicit/explicit object creation
dotnet_diagnostic.RCS1250.severity = none

# RCS1251: Remove unnecessary braces from record declaration
dotnet_diagnostic.RCS1251.severity = suggestion

# RCS1252: Normalize usage of infinite loop
dotnet_diagnostic.RCS1252.severity = none

# RCS1253: Format documentation comment summary
dotnet_diagnostic.RCS1253.severity = none

# RCS1254: Normalize format of enum flag value
dotnet_diagnostic.RCS1254.severity = suggestion

# RCS1255: Simplify argument null check
dotnet_diagnostic.RCS1255.severity = none

# RCS1256: Invalid argument null check
dotnet_diagnostic.RCS1256.severity = warning

# RCS1257: Use enum field explicitly
dotnet_diagnostic.RCS1257.severity = none

# RCS1258: Unnecessary enum flag
dotnet_diagnostic.RCS1258.severity = suggestion

# RCS1259: Remove empty syntax
dotnet_diagnostic.RCS1259.severity = suggestion

# RCS1260: Add/remove trailing comma
dotnet_diagnostic.RCS1260.severity = none

# RCS1261: Resource can be disposed asynchronously
dotnet_diagnostic.RCS1261.severity = warning

# RCS1262: Unnecessary raw string literal
dotnet_diagnostic.RCS1262.severity = suggestion

# RCS1263: Invalid reference in a documentation comment
dotnet_diagnostic.RCS1263.severity = warning

# RCS1264: Use 'var' or explicit type
dotnet_diagnostic.RCS1264.severity = none

# RCS1265: Remove redundant catch block
dotnet_diagnostic.RCS1265.severity = suggestion

# RCS1266: Use raw string literal
dotnet_diagnostic.RCS1266.severity = none

# RCS1267: Use string interpolation instead of 'string.Concat'
dotnet_diagnostic.RCS1267.severity = suggestion

# RCS1268: Simplify numeric comparison
dotnet_diagnostic.RCS1268.severity = suggestion

# RCS1269: Use explicit type instead of 'var'
dotnet_diagnostic.RCS1269.severity = none

# RCS1270: Throw default exception
dotnet_diagnostic.RCS1270.severity = none

# RCS1271: Format documentation comment summary on a single line
dotnet_diagnostic.RCS1271.severity = none

# RCS1272: Remove redundant string interpolation
dotnet_diagnostic.RCS1272.severity = suggestion
```

## Unity Analyzers (RCS9xxx)

These are specific to Unity development.

```ini
# RCS9001: Use predefined type
dotnet_diagnostic.RCS9001.severity = none

# RCS9002: Use predefined type
dotnet_diagnostic.RCS9002.severity = none

# RCS9003: Unnecessary conditional access
dotnet_diagnostic.RCS9003.severity = warning

# RCS9004: Call 'Any' instead of accessing 'Count'
dotnet_diagnostic.RCS9004.severity = warning

# RCS9005: Unnecessary null check
dotnet_diagnostic.RCS9005.severity = warning

# RCS9006: Use element access
dotnet_diagnostic.RCS9006.severity = suggestion

# RCS9007: Use return value
dotnet_diagnostic.RCS9007.severity = warning

# RCS9008: Call 'Last' instead of using []
dotnet_diagnostic.RCS9008.severity = suggestion

# RCS9009: Unknown language name
dotnet_diagnostic.RCS9009.severity = warning

# RCS9010: Specify ExportCodeRefactoringProviderAttribute.Name
dotnet_diagnostic.RCS9010.severity = warning

# RCS9011: Specify ExportCodeFixProviderAttribute.Name
dotnet_diagnostic.RCS9011.severity = warning

# RCS9012: Use pattern matching
dotnet_diagnostic.RCS9012.severity = suggestion
```

## Configuration Options

Some analyzers require additional configuration:

```ini
# Required for RCS1016
roslynator_body_style = block|expression|when_on_single_line

# Required for RCS1096
roslynator_enum_has_flag_style = method|operator

# Required for RCS1248
roslynator_null_check_style = pattern_matching

# Other options
roslynator_accessibility_modifiers = explicit|implicit
roslynator_use_anonymous_function_or_method_group = anonymous_function|method_group
roslynator_use_block_body_when_declaration_spans_over_multiple_lines = true|false
roslynator_use_block_body_when_expression_spans_over_multiple_lines = true|false
roslynator_blank_line_after_file_scoped_namespace_declaration = true|false
roslynator_blank_line_between_single_line_accessors = true|false
roslynator_blank_line_between_using_directives = never|separate_groups
roslynator_accessor_braces_style = multi_line|single_line_when_expression_is_on_single_line
roslynator_conditional_operator_condition_parentheses_style = include|omit|omit_when_condition_is_single_token
roslynator_configure_await = true|false
roslynator_empty_string_style = literal|field
roslynator_enum_flag_value_style = decimal_number|shift_operator
roslynator_equals_token_new_line = after|before
roslynator_infinite_loop_style = for|while
roslynator_max_line_length = <number>
roslynator_new_line_at_end_of_file = true|false
roslynator_new_line_before_while_in_do_statement = true|false
roslynator_null_conditional_operator_new_line = after|before
roslynator_object_creation_parentheses_style = include|omit
roslynator_object_creation_type_style = explicit|implicit|implicit_when_type_is_obvious
roslynator_prefix_field_identifier_with_underscore = true|false
roslynator_suppress_unity_script_methods = true|false
roslynator_use_collection_expression = true|false
roslynator_use_var_instead_of_implicit_object_creation = true|false
```

## Notes

- This list is based on Roslynator version 4.12.9
- Default severities are set to match common practices
- Adjust severities based on your team's preferences
- Some rules conflict with each other (e.g., RCS1010 vs RCS1176 for var usage)
- Rules can be overridden at the project or file level