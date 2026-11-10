# Agent Guidelines

- Use CsWin32 for Win32 APIs, native structures, constants, and handles. Add required symbols to `RegistryEditor/NativeMethods.txt` and use the generated `Windows.Win32` APIs instead of hand-written P/Invoke declarations or native structure definitions.
- Keep `RegistryEditor/NativeMethods.json` configured with `allowMarshaling: false`; prefer blittable signatures and unmanaged function pointers for callbacks.
