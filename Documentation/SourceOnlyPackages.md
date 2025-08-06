# Source-Only Packages Explained

## What is a Source-Only Package?

A source-only package is a NuGet package that contains ONLY source code files (.cs files), not compiled DLLs. When you reference a source-only package, the source files are copied into your project and compiled as part of YOUR assembly.

## How It Should Work

### 1. TimeWarp.Nuru.Parsing Package Configuration

```xml
<PropertyGroup>
  <IncludeBuildOutput>false</IncludeBuildOutput>  <!-- Don't include DLL in package -->
  <DevelopmentDependency>true</DevelopmentDependency>  <!-- Not a runtime dependency -->
</PropertyGroup>

<ItemGroup>
  <!-- Include all source files as content -->
  <Content Include="**/*.cs" Exclude="obj/**;bin/**">
    <Pack>true</Pack>
    <PackagePath>contentFiles/cs/any/TimeWarp.Nuru.Parsing/</PackagePath>
    <BuildAction>Compile</BuildAction>
  </Content>
</ItemGroup>
```

### 2. How TimeWarp.Nuru Should Reference It

**For Development (ProjectReference):**
```xml
<!-- This is WRONG - creates runtime dependency -->
<ProjectReference Include="..\TimeWarp.Nuru.Parsing\TimeWarp.Nuru.Parsing.csproj" />

<!-- This is what we need - compile source files directly -->
<ItemGroup>
  <Compile Include="..\TimeWarp.Nuru.Parsing\**\*.cs" Exclude="..\TimeWarp.Nuru.Parsing\obj\**;..\TimeWarp.Nuru.Parsing\bin\**" />
</ItemGroup>
```

**For NuGet Package Users (PackageReference):**
```xml
<PackageReference Include="TimeWarp.Nuru.Parsing" Version="2.1.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>contentfiles;build</IncludeAssets>
</PackageReference>
```

### 3. How TimeWarp.Nuru.Analyzers Uses It

The analyzer already does this correctly:
```xml
<ItemGroup>
  <Compile Include="..\TimeWarp.Nuru.Parsing\**\*.cs" Exclude="..\TimeWarp.Nuru.Parsing\obj\**;..\TimeWarp.Nuru.Parsing\bin\**" />
</ItemGroup>
```

## The Current Problem

1. TimeWarp.Nuru uses `ProjectReference` which creates a runtime dependency
2. Even with `PrivateAssets="all"`, ProjectReference still expects the DLL at runtime
3. This is why we get FileNotFoundException

## The Solution

1. Change TimeWarp.Nuru to compile the parsing source files directly (like the analyzer does)
2. Keep TimeWarp.Nuru.Parsing as a source-only package
3. Don't publish TimeWarp.Nuru.Parsing to NuGet (it's only needed at compile time)

## Result

- TimeWarp.Nuru.dll will contain all the parsing code compiled into it
- TimeWarp.Nuru.Analyzers.dll will also contain all the parsing code compiled into it
- No runtime dependency on TimeWarp.Nuru.Parsing.dll
- The parsing package is just a convenient way to share source code between projects