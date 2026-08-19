# Lua 5.1.5 runtime

The Windows runtime is Lua 5.1.5 with `LUA_COMPAT_VARARG` disabled. This keeps
Lua.NET's `lua515` ABI while matching WoW's handling of named parameters in
vararg functions.

Run `build-win-x64.ps1` from a Visual Studio developer machine to rebuild it
from the official Lua source archive.
