#!/bin/bash
# AOT Benchmark: Compare all CLI frameworks
# Run from benchmarks/aot-benchmarks directory

cd "$(dirname "$0")"

hyperfine --warmup 10 --runs 100 -N \
  -n 'ConsoleAppFramework' 'publish/bench-consoleappframework/bench-consoleappframework --str hello --i 13 --b' \
  -n 'System.CommandLine' 'publish/bench-systemcommandline/bench-systemcommandline --str hello -i 13 -b' \
  -n 'CommandLineParser' 'publish/bench-commandlineparser/bench-commandlineparser --str hello -i 13 -b' \
  -n 'Nuru-Direct' 'publish/bench-nuru-direct/bench-nuru-direct --str hello -i 13 -b' \
  -n 'CoconaLite' 'publish/bench-coconalite/bench-coconalite --str hello -i 13 -b' \
  -n 'SpectreConsole' 'publish/bench-spectreconsole/bench-spectreconsole --str hello -i 13 -b' \
  -n 'Nuru-Full' 'publish/bench-nuru-full/bench-nuru-full --str hello -i 13 -b' \
  -n 'Cocona' 'publish/bench-cocona/bench-cocona --str hello -i 13 -b'
