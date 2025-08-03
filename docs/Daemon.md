# Daemon

For speech quality reasons the WinRT API needs to be used, however because of framework and
compiler shenanigans it felt simpler to make a daemon serving TTS requests.

The main UI spawns this daemon as a child process and they communicate using a named pipe.
If the daemon terminates (e.g. in case of an internal error) then a new one is spawned.

The information is packed as binary.

Both the UI and the daemon are expected to read exactly as many bytes as the other side has 
written. The exact packing is abstracted by records and extensions in the `.Protocol` namespace
for ease of use and type safety.

The communication is stateless (akin to http). The daemon does not distinguish between clients.
