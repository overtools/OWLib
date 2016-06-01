#!/bin/sh
git submodule update --init
pushd CASCExplorer
git reset --hard HEAD
popd
patch --no-backup-if-mismatch -fsr /dev/null --verbose CASCExplorer/CascLib/Logger.cs CascLib.patch/Logger.cs.diff || cp -f CascLib.patch/Logger.cs CASCExplorer/CascLib/Logger.cs
patch --no-backup-if-mismatch -fsr /dev/null --verbose CASCExplorer/CascLib/PerfCounter.cs CascLib.patch/PerfCounter.cs.diff || cp -f CascLib.patch/PerfCounter.cs CASCExplorer/CascLib/PerfCounter.cs
