#!/bin/sh
git submodule update --init
patch --no-backup-if-mismatch -fsr /dev/null --verbose CASCExplorer/CascLib/Logger.cs CascLib.patch/Logger.cs.diff || cp -f CascLib.patch/Logger.cs CASCExplorer/CascLib/Logger.cs
